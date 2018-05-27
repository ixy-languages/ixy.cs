using System;
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
                //View accessor for mmmapped file is automatically created
                PciMemMap = PciController.MapResource(pciAddr);
            }
            catch(Exception e)
            {
                Log.Error("FATAL: Could not map memory for device {0} - {1}", pciAddr, e.Message);
                Environment.Exit(1);
            }

            //TODO : Initialize rx / tx queue arrays (which type?)

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

        public override uint RxBatch(int queueId, PacketBuffer[] buffers)
        {
            //TODO : Implement
            return 0;
        }

        public override uint TxBatch(int queueId, PacketBuffer[] buffers)
        {
            //TODO : Implement
            return 0;
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

            //TODO : Foreach queue : start queue (tx and rx)

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
            //TODO : USE NUM OF QUEUES
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
                IntPtr ringPtr = Marshal.AllocHGlobal(ringSizeBytes);
                DmaMemory dmaMem = new DmaMemory(ringPtr, MemoryHelper.VirtToPhys(ringPtr));
                //TODO : The C version sets the allocated memory to -1 here

                //TODO: What's the point of the masking here?
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
                int ringSizeBytes = NumTxQueueEntries * TxDescriptorSize;
                IntPtr ringPtr = Marshal.AllocHGlobal(ringSizeBytes);
                DmaMemory dmaMem = new DmaMemory(ringPtr, MemoryHelper.VirtToPhys(ringPtr));
                //TODO : The C version sets the allocated memory to -1 here
                //TODO : What's the point of the masking here?
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

        private void StartRxQueue(int i)
        {
            Log.Notice("Starting RX queue {0}", i);
            IxgbeRxQueue queue = (IxgbeRxQueue)RxQueues[i];
            //Mempool should be >= number of rx and tx descriptors
            int mempoolSize = NumRxQueueEntries + NumTxQueueEntries;
        }
    }
}