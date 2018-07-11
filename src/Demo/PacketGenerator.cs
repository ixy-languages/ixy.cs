using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using IxyCs;
using IxyCs.Ixgbe;
using IxyCs.Memory;

public class PacketGenerator
{
    private const int BuffersCount = 2048;
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

    private Mempool _mempool;

    public PacketGenerator(string pciAddr)
    {
        InitMempool();
        var dev = new IxgbeDevice(pciAddr, 1, 1);

        var statsOld = new DeviceStats(dev);
        var statsNew = new DeviceStats(dev);
        ulong counter = 0;
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        var buffers = new PacketBuffer[BatchSize];
        int seqNum = 0;

        while(true)
        {
            buffers = _mempool.AllocatePacketBuffers(BatchSize);
            foreach(var buf in buffers)
                Marshal.WriteInt32(IntPtr.Add(buf.VirtualAddress, PacketSize - 4), seqNum);
            dev.TxBatchBusyWait(0, buffers);

            if((counter++ & 0xFFF) == 0 && stopWatch.ElapsedMilliseconds > 100)
            {
                stopWatch.Stop();
                var nanos = stopWatch.ElapsedTicks;
                dev.ReadStats(ref statsNew);
                statsNew.PrintStatsDiff(ref statsOld, (ulong)nanos);
                statsOld = statsNew;
                counter = 0;
                stopWatch.Restart();
            }
        }
    }

    private void InitMempool()
    {
        _mempool = MemoryHelper.AllocateMempool(BuffersCount, 0);

        //Pre-fill all our packet buffers with some templates that can be modified later
        var buffers = new PacketBuffer[BuffersCount];
        for(int i = 0; i < BuffersCount; i++)
        {
            var buffer = _mempool.AllocatePacketBuffer();
            buffer.Size = PacketSize;
            Marshal.Copy(PacketData, 0, buffer.VirtualAddress, PacketData.Length);
            var ipData = new byte[20];
            Marshal.Copy(IntPtr.Add(buffer.VirtualAddress, PacketBuffer.DataOffset + 14),
            ipData, 0, 20);
            Marshal.WriteInt16(IntPtr.Add(buffer.VirtualAddress, 
                PacketBuffer.DataOffset + 24), (short)CalcIpChecksum(ipData));
            buffers[i] = buffer;
        }

        //Return them all to the mempool, all future allocations will return buffers with the data set above
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