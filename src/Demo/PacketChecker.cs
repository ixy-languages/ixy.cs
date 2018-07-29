using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using IxyCs;
using IxyCs.Ixgbe;
using IxyCs.Memory;

namespace IxyCs.Demo
{
    public class PacketChecker
    {
        private const int BuffersCount = 64;
        private const int PacketSize = 60;
        private const int BatchSize = 64;

        private readonly byte[] PacketData = new byte[] {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, //dst MAC
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, //src MAC
            0x08, 0x00, //ether type: IPv4
            0x45, 0x00, //Version IHL, TOS
            (PacketSize - 14) >> 8, //ip len excluding ethernet, high byte
            (PacketSize - 14) & 0xFF, //ip len excluding ethernet, low byte
            0x00, 0x00, 0x00, 0x00, //id,flags,fragmentation
            0x40, 0x11, 0x00, 0x00, //TTL(64), protocol (UDP), checksum
            0x0A, 0x00, 0x00, 0x01,             // src ip (10.0.0.1)
            0x0A, 0x00, 0x00, 0x02,             // dst ip (10.0.0.2)
            0x00, 0x2A, 0x05, 0x39,             // src and dst ports (42 -> 1337)
            (PacketSize - 20 - 14) >> 8,          // udp len excluding ip & ethernet, high byte
            (PacketSize - 20 - 14) & 0xFF,        // udp len exlucding ip & ethernet, low byte
            0x00, 0x00,                         // udp checksum, optional
            0x69, 0x78, 0x79                       // payload ("ixy")
            // rest of the payload is zero-filled because mempools guarantee empty bufs
        };

        //24 random bytes of extra data to simulate how a timestamped packet might look
        private readonly byte[] ExtraData = new byte[] {
            0x00, 0x01, 0x02, 0x03, 0x03, 0x05, 0x06, 0x07,
            0xA0, 0xA1, 0xA2, 0xA3, 0xA3, 0xA5, 0xA6, 0xA7,
            0xB0, 0xB1, 0xB2, 0xB3, 0xB3, 0xB5, 0xB6, 0xB7,
        };

        private Mempool _mempool;

        public PacketChecker(string pciAddr, string mode)
        {
            if(mode == "rx")
                RecPackets(pciAddr);
            else
                SendPackets(pciAddr);
        }

        private void SendPackets(string pciAddr)
        {
            var dev = new IxgbeDevice(pciAddr, 1, 1);
            InitMempool();
            var buffers = _mempool.GetPacketBuffers(BatchSize);
            int seqNum = 0;
            foreach(var buf in buffers)
            {
                buf.WriteData(PacketSize - 4, seqNum++);
                if(buf.Size == 84)
                    Console.WriteLine("Large buffer has seq num {0}", seqNum - 1);
            }
            dev.TxBatchBusyWait(0, buffers);
        }

        private void RecPackets(string pciAddr)
        {
            Console.WriteLine("Waiting for packets...");
            var dev = new IxgbeDevice(pciAddr, 1, 1);
            while(true)
            {
                var buffers = dev.RxBatch(0, BatchSize);
                if(buffers.Length < 1)
                    continue;
                Console.WriteLine("Received {0} packets", buffers.Length);
                var sizes = buffers.Select(buf => buf.Size).Distinct();
                Console.WriteLine("Buffer sizes are: ", string.Join("/", sizes));
                var large = buffers.Where(buf => buf.Size == 84);
                Console.WriteLine("{0} 84 byte packets received", large.Count());
                if(large.Count() != 0)
                {
                    var buf = large.First();
                    buf.DebugPrint();
                    return;
                }
            }
        }

        private void InitMempool()
        {
            _mempool = MemoryHelper.AllocateMempool(BuffersCount);

            //Pre-fill all our packet buffers with some templates that can be modified later
            var buffers = new PacketBuffer[BuffersCount];
            for(int i = 0; i < BuffersCount; i++)
            {
                var buffer = _mempool.GetPacketBuffer();
                //One random packet has additional data
                buffer.Size = (i == 50) ? PacketData.Length + ExtraData.Length : PacketData.Length;
                buffer.WriteData(0, PacketData);
                if(i == 50)
                    buffer.WriteData(PacketData.Length, ExtraData);
                var ipData = buffer.CopyData(14, 20);
                buffer.WriteData(24, (short)CalcIpChecksum(ipData));
                buffers[i] = buffer;
            }

            //Return them all to the mempool, all future allocations will return buffers with the data set above
            //TODO : Not sure if the order is correct here
            foreach(var buffer in buffers)
                _mempool.FreeBuffer(buffer);
        }

        private ushort CalcIpChecksum(byte[] data)
        {
            if(data.Length % 2 != 0)
            {
                Log.Error("Odd sized checksums NYI");
                Environment.Exit(1);
            }
            uint checksum = 0;
            for(uint i = 0; i < data.Length / 2; i++)
            {
                checksum += (uint)data[i];
                if(checksum > 0xFFFF)
                    checksum = (checksum & 0xFFFF) + 1; //16 bit one's complement
            }
            return (ushort)(~((ushort)checksum));
        }
    }
}