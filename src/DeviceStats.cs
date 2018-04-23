using System;
namespace IxyCs
{
    public struct DeviceStats
    {
        public readonly IxyDevice Device;
        public uint RxPackets;
        public uint TxPackets;
        public ulong RxBytes;
        public ulong TxBytes;


        public static double DiffMpps(ulong packetsNew, ulong packetsOld, ulong nanos)
        {
            return (double)(packetsNew - packetsOld) / 1000000.0 / ((double)nanos / 1000000000.0);
        }

        public static uint DiffMbit(ulong bytesNew, ulong bytesOld, ulong packetsNew, ulong packetsOld, ulong nanos)
        {
            //10000 mbit/s + preamble
            return (uint) (((bytesNew - bytesOld) / 1000000.0 / ((double)nanos / 1000000000.0)) * 8
                           + DiffMpps(packetsNew, packetsOld, nanos) * 20 * 8);
        }

        public DeviceStats(IxyDevice dev, uint rxp, uint txp, ulong rxb, ulong txb)
        {
            this.Device = dev;
            this.RxPackets = rxp;
            this.TxPackets = txp;
            this.RxBytes = rxb;
            this.TxBytes = txb;
        }

        public void PrintStats()
        {
            Console.WriteLine("{0} RX: {1} bytes {2} packets", Device?.PciAddress, RxBytes, RxPackets);
            Console.WriteLine("{0} TX: {1} bytes {2} packets", Device?.PciAddress, TxBytes, TxPackets);
        }
    }
}
