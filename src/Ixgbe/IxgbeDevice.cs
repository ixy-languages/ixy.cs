using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using IxyCs.Memory;
using IxyCs.Pci;

namespace IxyCs.Ixgbe
{
    public class IxgbeDevice : IxyDevice
    {
        public const int MaxQueues = 64;
        private const int MaxRxQueueEntries = 4096;
        private const int MaxTxQueueEntries = 4096;
        private const int NumRxQueueEntries = 512;
        private const int NumTxQueueEntries = 512;
        private const int RxDescriptorSize = 16;
        private const int TxDescriptorSize = 16;
        private const int TxCleanBatch = 32;

        public IxgbeDevice(string pciAddr, int rxQueues, int txQueues)
            :base(pciAddr, rxQueues, txQueues)
        {
            if(txQueues < 0 || txQueues > MaxQueues)
                throw new ArgumentException(String.Format("Cannot configure {0} tx queues - limit is {1}",
                 txQueues, MaxQueues));
            if(rxQueues < 0 || rxQueues > MaxQueues)
                throw new ArgumentException(String.Format("Cannot configure {0} rx queues - limit is {1}",
                 rxQueues, MaxQueues));

            DriverName = "ixy-ixgbe";

            RxQueues = new IxgbeRxQueue[rxQueues];
            TxQueues = new IxgbeTxQueue[txQueues];

            PciController.RemoveDriver(pciAddr);
            PciController.EnableDma(pciAddr);
            try
            {
                //View accessor for mmapped file is automatically created
                PciMemMap = PciController.MapResource(pciAddr);
            }
            catch(Exception e)
            {
                Log.Error("FATAL: Could not map memory for device {0} - {1}", pciAddr, e.Message);
                Environment.Exit(1);
            }

            ResetAndInit();
        }

        public override uint GetLinkSpeed()
        {
            uint links = GetReg(IxgbeDefs.LINKS);
            if((links & IxgbeDefs.LINKS_UP) == 0)
                return 0;

            switch(links & IxgbeDefs.LINKS_SPEED_82599)
            {
                case IxgbeDefs.LINKS_SPEED_100_82599:
                    return 100;
                case IxgbeDefs.LINKS_SPEED_1G_82599:
                    return 1000;
                case IxgbeDefs.LINKS_SPEED_10G_82599:
                    return 10000;
                default:
                    Log.Warning("Got unknown link speed");
                    return 0;
            }
        }

        public override void SetPromisc(bool enabled)
        {
            if(enabled)
            {
                Log.Notice("Enabling promisc mode");
                SetFlags(IxgbeDefs.FCTRL, IxgbeDefs.FCTRL_MPE | IxgbeDefs.FCTRL_UPE);
            }
            else
            {
                Log.Notice("Disabling promisc mode");
                ClearFlags(IxgbeDefs.FCTRL, IxgbeDefs.FCTRL_MPE | IxgbeDefs.FCTRL_UPE);
            }
            PromiscEnabled = enabled;
        }

        public override void ReadStats(ref DeviceStats stats)
        {
            uint rxPackets = GetReg(IxgbeDefs.GPRC);
            uint txPackets = GetReg(IxgbeDefs.GPTC);
            ulong rxBytes = GetReg(IxgbeDefs.GORCL) + (((ulong)GetReg(IxgbeDefs.GORCH)) << 32);
            ulong txBytes = GetReg(IxgbeDefs.GOTCL) + (((ulong)GetReg(IxgbeDefs.GOTCH)) << 32);
            stats.RxPackets += rxPackets;
            stats.TxPackets += txPackets;
            stats.RxBytes += rxBytes;
            stats.TxBytes += txBytes;
        }

        //Section 1.8.2 and 7.1
        //Try to receive a single packet if one is available, non-blocking
        //Section 7.1.9 explains RX ring structure
        //We control the tail of the queue, hardware controls the head
        public override PacketBuffer[] RxBatch(int queueId, int buffersCount)
        {
            if(queueId < 0 || queueId >= RxQueues.Length)
                throw new ArgumentOutOfRangeException("Queue id out of bounds");

            var buffers = new PacketBuffer[buffersCount];
            var queue = RxQueues[queueId] as IxgbeRxQueue;
            ushort rxIndex = (ushort)queue.Index;
            ushort lastRxIndex = rxIndex;
            int bufInd;
            for(bufInd = 0; bufInd < buffersCount; bufInd++)
            {
                var status = queue.ReadWbStatusError(rxIndex);
                //Status DONE
                if((status & IxgbeDefs.RXDADV_STAT_DD) != 0)
                {
                    //Status END OF PACKET
                    if((status & IxgbeDefs.RXDADV_STAT_EOP) == 0)
                        throw new InvalidOperationException("Multi segment packets are not supported - increase buffer size or decrease MTU");

                    //We got a packet - read and copy the whole descriptor
                    var packetBuffer = new PacketBuffer(queue.VirtualAddresses[rxIndex]);

                    packetBuffer.Size = queue.ReadWbLength(rxIndex);

                    //This would be the place to implement RX offloading by translating the device-specific
                    //flags to an independent representation in that buffer (similar to how DPDK works)
                    var newBuf = queue.Mempool.GetPacketBufferFast();
                    if(newBuf.IsNull)
                    {
                        Log.Error("Cannot allocate RX buffer - Out of memory! Either there is a memory leak, or the mempool is too small");
                        throw new OutOfMemoryException("Failed to allocate new buffer for rx - you are either leaking memory or your mempool is too small");
                    }

                    queue.WriteBufferAddress(rxIndex, newBuf.PhysicalAddress + PacketBuffer.DataOffset);
                    queue.WriteHeaderBufferAddress(rxIndex, 0); //This resets the flags
                    queue.VirtualAddresses[rxIndex] = newBuf.VirtualAddress;
                    buffers[bufInd] = packetBuffer;

                    //Want to read the next one in the next iteration but we still need the current one to update RDT later
                    lastRxIndex = rxIndex;
                    rxIndex = WrapRing(rxIndex, (ushort)queue.EntriesCount);
                }
                else {break;}
            }

            if(rxIndex != lastRxIndex)
            {
                //Tell hardware that we are done. This is intentionally off by one, otherwise we'd set
                //RDT=RDH if we are receiving faster than packets are coming in, which would mean queue is full
                SetReg(IxgbeDefs.RDT((uint)queueId), lastRxIndex);
                queue.Index = rxIndex;
            }
            Array.Resize(ref buffers, bufInd);
            return buffers;
        }

        public override int TxBatch(int queueId, PacketBuffer[] buffers)
        {
            if(queueId < 0 || queueId >= RxQueues.Length)
                throw new ArgumentOutOfRangeException("Queue id out of bounds");

            var queue = TxQueues[queueId] as IxgbeTxQueue;
            ushort cleanIndex = queue.CleanIndex;
            ushort currentIndex = (ushort)queue.Index;
            var cmdTypeFlags = IxgbeDefs.ADVTXD_DCMD_EOP | IxgbeDefs.ADVTXD_DCMD_RS | IxgbeDefs.ADVTXD_DCMD_IFCS |
                                        IxgbeDefs.ADVTXD_DCMD_DEXT | IxgbeDefs.ADVTXD_DTYP_DATA;
            //All packet buffers that will be handled here will belong to the same mempool
            Mempool pool = null;

            //Step 1: Clean up descriptors that were sent out by the hardware and return them to the mempool
            //Start by reading step 2 which is done first for each packet
            //Cleaning up must be done in batches for performance reasons, so this is unfortunately somewhat complicated
            while(true)
            {
                //currentIndex is always ahead of clean (invariant of our queue)
                int cleanable = currentIndex - cleanIndex;
                if(cleanable < 0)
                    cleanable = queue.EntriesCount + cleanable;
                if(cleanable < TxCleanBatch)
                    break;

                //Calculate the index of the last transcriptor in the clean batch
                //We can't check all descriptors for performance reasons
                int cleanupTo = cleanIndex + TxCleanBatch - 1;
                if(cleanupTo >= queue.EntriesCount)
                    cleanupTo -= queue.EntriesCount;

                ushort descIndex = (ushort)cleanupTo;
                uint status = queue.ReadWbStatus(descIndex);

                //Hardware sets this flag as soon as it's sent out, we can give back all bufs in the batch back to the mempool
                if((status & IxgbeDefs.ADVTXD_STAT_DD) != 0)
                {
                    int i = cleanIndex;
                    while(true)
                    {
                        var packetBuffer = new PacketBuffer(queue.VirtualAddresses[i]);
                        if(pool == null)
                        {
                            pool = Mempool.FindPool(packetBuffer.MempoolId);
                            if(pool == null)
                                throw new NullReferenceException("Could not find mempool with id specified by PacketBuffer");
                        }
                        pool.FreeBufferFast(packetBuffer);
                        if(i == cleanupTo)
                            break;
                        i = WrapRing(i, queue.EntriesCount);
                    }
                    //Next descriptor to be cleaned up is one after the one we just cleaned
                    cleanIndex = (ushort)WrapRing(cleanupTo, queue.EntriesCount);
                }
                //Clean the whole batch or nothing. This will leave some packets in the queue forever
                //if you stop transmitting but that's not a real concern
                else {break;}
            }
            queue.CleanIndex = cleanIndex;

            //Step 2: Send out as many of our packets as possible
            uint sent;
            for(sent = 0; sent < buffers.Length; sent++)
            {
                ushort nextIndex = WrapRing(currentIndex, (ushort)queue.EntriesCount);
                //We are full if the next index is the one we are trying to reclaim
                if(cleanIndex == nextIndex)
                    break;

                var buffer = buffers[sent];
                //Remember virtual address to clean it up later
                queue.VirtualAddresses[currentIndex] = buffer.VirtualAddress;
                queue.Index = WrapRing(queue.Index, queue.EntriesCount);
                //NIC reads from here
                queue.WriteBufferAddress(currentIndex, buffer.PhysicalAddress + PacketBuffer.DataOffset);
                //Always the same flags: One buffer (EOP), advanced data descriptor, CRC offload, data length
                var bufSize = buffer.Size;
                queue.WriteCmdTypeLength(currentIndex, cmdTypeFlags | bufSize);
                //No fancy offloading - only the total payload length
                //implement offloading flags here:
                // * ip checksum offloading is trivial: just set the offset
                // * tcp/udp checksum offloading is more annoying, you have to precalculate the pseudo-header checksum
                queue.WriteOlInfoStatus(currentIndex, bufSize << (int)IxgbeDefs.ADVTXD_PAYLEN_SHIFT);
                currentIndex = nextIndex;
            }

            //Send out by advancing tail, i.e. pass control of the bus to the NIC
            SetReg(IxgbeDefs.TDT((uint)queueId), (uint)queue.Index);
            return (int)sent;
        }

        private void ResetAndInit()
        {
            Log.Notice("Resetting device {0}", PciAddress);

            //Sec 4.6.3.1 - Disable all interrupts
            SetReg(IxgbeDefs.EIMC, 0x7FFFFFFF);

            //Sec 4.6.3.2 - Global reset (software + link)
            SetReg(IxgbeDefs.CTRL, IxgbeDefs.CTRL_RST_MASK);
            WaitClearReg(IxgbeDefs.CTRL, IxgbeDefs.CTRL_RST_MASK);
            //Wait 0.01 seconds for reset
            Thread.Sleep(10);

            //Sec 4.6.3.1 - Disable interrupts again after reset
            SetReg(IxgbeDefs.EIMC, 0x7FFFFFFF);

            Log.Notice("Initializing device {0}", PciAddress);

            //Sec 4.6.3 - Wait for EEPROM auto read completion
            WaitSetReg(IxgbeDefs.EEC, IxgbeDefs.EEC_ARD);

            //Sec 4.6.3 - Wait for DMA initialization to complete
            WaitSetReg(IxgbeDefs.RDRXCTL, IxgbeDefs.RDRXCTL_DMAIDONE);

            //Sec 4.6.4 - Init link (auto negotiation)
            InitLink();

            //Sec 4.6.5 - Statistical counters
            //Reset-on-read registers, just read them once
            var stats = new DeviceStats();
            ReadStats(ref stats);

            //Sec 4.6.7 - Init Rx
            InitRx();

            //Sec 4.6.8 - Init Tx
            InitTx();

            //Start each Rx/Tx queue
            for(int i = 0; i < RxQueues.Length; i++)
                StartRxQueue(i);
            for(int i= 0; i < TxQueues.Length; i++)
                StartTxQueue(i);

            //Skipping last step from 4.6.3
            SetPromisc(true);

            WaitForLink();

        }

        private void InitLink()
        {
            //Should already be set by the eeprom config
            SetReg(IxgbeDefs.AUTOC, (GetReg(IxgbeDefs.AUTOC) & ~IxgbeDefs.AUTOC_LMS_MASK) | IxgbeDefs.AUTOC_LMS_10G_SERIAL);
            SetReg(IxgbeDefs.AUTOC, GetReg(IxgbeDefs.AUTOC) & ~IxgbeDefs.AUTOC_10G_PMA_PMD_MASK | IxgbeDefs.AUTOC_10G_XAUI);

            //Negotiate link
            SetFlags(IxgbeDefs.AUTOC, IxgbeDefs.AUTOC_AN_RESTART);
        }

        private void WaitForLink()
        {
            Log.Notice("Waiting for link...");
            int waited = 0;
            uint speed;
            //Wait up to 10 seconds for link (GetLinkSpeed returns 0 until link is established)
            while((speed = GetLinkSpeed()) == 0 && waited < 10000)
            {
                Thread.Sleep(10);
                waited += 10;
            }
            if(speed != 0)
                Log.Notice("Link established - speed is {0} Mbit/s", speed);
            else
                Log.Warning("Timed out while waiting for link");

        }

        private void InitRx()
        {
            //Disable RX while re-configuring
            ClearFlags(IxgbeDefs.RXCTRL, IxgbeDefs.RXCTRL_RXEN);

            //No DCB or VT, just a single 128kb packet buffer
            SetReg(IxgbeDefs.RXPBSIZE(0), IxgbeDefs.RXPBSIZE_128KB);
            for(uint i = 1; i < 8; i++)
                SetReg(IxgbeDefs.RXPBSIZE(i), 0);

            //Always enable CRC offloading
            SetFlags(IxgbeDefs.HLREG0, IxgbeDefs.HLREG0_RXCRCSTRP);
            SetFlags(IxgbeDefs.RDRXCTL, IxgbeDefs.RDRXCTL_CRCSTRIP);

            //Accept broadcast packets
            SetFlags(IxgbeDefs.FCTRL, IxgbeDefs.FCTRL_BAM);

            //Per queue config
            for(uint i = 0; i < RxQueues.Length; i++)
            {
                Log.Notice("Initializing rx queue {0}", i);
                //Enable advanced rx descriptors
                SetReg(IxgbeDefs.SRRCTL(i), (GetReg(IxgbeDefs.SRRCTL(i)) & ~IxgbeDefs.SRRCTL_DESCTYPE_MASK)
                                            | IxgbeDefs.SRRCTL_DESCTYPE_ADV_ONEBUF);
                //DROP_EN causes the NIC to drop packets if no descriptors are available instead of buffering them
                //A single overflowing queue can fill up the whole buffer and impact operations if not setting this
                SetFlags(IxgbeDefs.SRRCTL(i), IxgbeDefs.SRRCTL_DROP_EN);

                //Sec 7.1.9 - Set up descriptor ring
                int ringSizeBytes = NumRxQueueEntries * RxDescriptorSize;
                var dmaMem = MemoryHelper.AllocateDmaC((uint)ringSizeBytes, true);
                //TODO : The C version sets the allocated memory to -1 here

                SetReg(IxgbeDefs.RDBAL(i), (uint)(dmaMem.PhysicalAddress & 0xFFFFFFFFL));
                SetReg(IxgbeDefs.RDBAH(i), (uint)(dmaMem.PhysicalAddress >> 32));
                SetReg(IxgbeDefs.RDLEN(i), (uint)ringSizeBytes);
                Log.Notice("RX ring {0} physical address: {1}", i, dmaMem.PhysicalAddress);
                Log.Notice("RX ring {0} virtual address: {1}", i, dmaMem.VirtualAddress);

                //Set ring to empty
                SetReg(IxgbeDefs.RDH(i), 0);
                SetReg(IxgbeDefs.RDT(i), 0);

                var queue = new IxgbeRxQueue(NumRxQueueEntries);
                queue.Index = 0;
                queue.DescriptorsAddr = dmaMem.VirtualAddress;
                RxQueues[i] = queue;
            }
            //Section 4.6.7 - set some magic bits
            SetFlags(IxgbeDefs.CTRL_EXT, IxgbeDefs.CTRL_EXT_NS_DIS);
            //This flag probably refers to a broken feature: It's reserved and initialized as '1' but it must be '0'
            for(uint i = 0; i < RxQueues.Length; i++)
                ClearFlags(IxgbeDefs.DCA_RXCTRL(i), 1 << 12);

            //Start RX
            SetFlags(IxgbeDefs.RXCTRL, IxgbeDefs.RXCTRL_RXEN);
        }

        //Section 4.6.8
        private void InitTx()
        {
            //CRC offload and small packet padding
            SetFlags(IxgbeDefs.HLREG0, IxgbeDefs.HLREG0_TXCRCEN | IxgbeDefs.HLREG0_TXPADEN);

            //Set default buffer size allocations (section 4.6.11.3.4)
            SetReg(IxgbeDefs.TXPBSIZE(0), IxgbeDefs.TXPBSIZE_40KB);
            for(uint i = 1; i < 8; i++)
                SetReg(IxgbeDefs.TXPBSIZE(i), 0);

            //Required when not using DCB/VTd
            SetReg(IxgbeDefs.DTXMXSZRQ, 0xFFFF);
            ClearFlags(IxgbeDefs.RTTDCS, IxgbeDefs.RTTDCS_ARBDIS);

            //Per queue config for all queues
            for(uint i = 0; i < TxQueues.Length; i++)
            {
                Log.Notice("Initializing TX queue {0}", i);

                //Section 7.1.9 - Setup descriptor ring
                uint ringSizeBytes = NumTxQueueEntries * TxDescriptorSize;
                var dmaMem = MemoryHelper.AllocateDmaC(ringSizeBytes, true);
                //TODO : The C version sets the allocated memory to -1 here
                SetReg(IxgbeDefs.TDBAL(i), (uint)(dmaMem.PhysicalAddress & 0xFFFFFFFFL));
                SetReg(IxgbeDefs.TDBAH(i), (uint)(dmaMem.PhysicalAddress >> 32));
                SetReg(IxgbeDefs.TDLEN(i), (uint)ringSizeBytes);
                Log.Notice("TX ring {0} physical addr: {1}", i, dmaMem.PhysicalAddress);
                Log.Notice("TX ring {0} virtual addr: {1}", i, dmaMem.VirtualAddress);

                //Descriptor writeback magic values, important to get good performance and low PCIe overhead
                //See sec. 7.2.3.4.1 and 7.2.3.5
                uint txdctl = GetReg(IxgbeDefs.TXDCTL(i));

                //Seems like overflow is irrelevant here
                unchecked
                {
                    //Clear bits
                    txdctl &= (uint)(~(0x3F | (0x3F << 8) | (0x3F << 16)));
                    //From DPDK
                    txdctl |= (36 | (8 << 8) | (4 << 16));
                }
                SetReg(IxgbeDefs.TXDCTL(i), txdctl);

                var queue = new IxgbeTxQueue(NumTxQueueEntries);
                queue.Index = 0;
                queue.DescriptorsAddr = dmaMem.VirtualAddress;
                TxQueues[i] = queue;
            }
            //Enable DMA
            SetReg(IxgbeDefs.DMATXCTL, IxgbeDefs.DMATXCTL_TE);
        }

        private void StartRxQueue(int queueId)
        {
            Log.Notice("Starting RX queue {0}", queueId);
            var queue = (IxgbeRxQueue)RxQueues[queueId];
            //Mempool should be >= number of rx and tx descriptors
            uint mempoolSize = NumRxQueueEntries + NumTxQueueEntries;
            queue.Mempool = MemoryHelper.AllocateMempool(mempoolSize < 4096 ? 4096 : mempoolSize, 2048);

            if((queue.EntriesCount & (queue.EntriesCount - 1)) != 0)
            {
                Log.Error("FATAL: number of queue entries must be a power of 2");
                Environment.Exit(1);
            }

            for(ushort ei = 0; ei < queue.EntriesCount; ei++)
            {
                Log.Notice("Setting up descriptor at index #{0}", ei);
                //Allocate packet buffer
                var packetBuffer = queue.Mempool.GetPacketBufferFast();
                if(packetBuffer.IsNull)
                {
                    Log.Error("Fatal: Could not allocate packet buffer");
                    Environment.Exit(1);
                }
                queue.WriteBufferAddress(ei, packetBuffer.PhysicalAddress + PacketBuffer.DataOffset);
                queue.WriteHeaderBufferAddress(ei, 0);
                queue.VirtualAddresses[ei] = packetBuffer.VirtualAddress;
            }

            //Enable queue and wait if necessary
            SetFlags(IxgbeDefs.RXDCTL((uint)queueId), IxgbeDefs.RXDCTL_ENABLE);
            WaitSetReg(IxgbeDefs.RXDCTL((uint)queueId), IxgbeDefs.RXDCTL_ENABLE);

            //Rx queue starts out full
            SetReg(IxgbeDefs.RDH((uint)queueId), 0);
            //Was set to 0 before in the init function
            SetReg(IxgbeDefs.RDT((uint)queueId), (uint)(queue.EntriesCount - 1));
        }

        private void StartTxQueue(int queueId)
        {
            Log.Notice("Starting TX queue {0}", queueId);
            var queue = (IxgbeTxQueue)TxQueues[queueId];

             if((queue.EntriesCount & (queue.EntriesCount - 1)) != 0)
            {
                Log.Error("FATAL: number of queue entries must be a power of 2");
                Environment.Exit(1);
            }

            //TX queue starts out empty
            SetReg(IxgbeDefs.TDH((uint)queueId), 0);
            SetReg(IxgbeDefs.TDT((uint)queueId), 0);

            //Enable queue and wait if necessary
            SetFlags(IxgbeDefs.TXDCTL((uint)queueId), IxgbeDefs.TXDCTL_ENABLE);
            WaitSetReg(IxgbeDefs.TXDCTL((uint)queueId), IxgbeDefs.TXDCTL_ENABLE);
        }

        //Advance index with wrap-around
        private ushort WrapRing(ushort index, ushort ringSize)
        {
            return (ushort)((index + 1) & (ringSize - 1));
        }

        private int WrapRing(int index, int ringSize)
        {
            return (index + 1) & (ringSize - 1);
        }
    }
}