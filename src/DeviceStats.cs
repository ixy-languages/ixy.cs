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

        public DeviceStats(IxyDevice dev)
        {
            this.Device = dev;
            this.RxBytes = 0;
            this.TxBytes = 0;
            this.RxPackets = 0;
            this.TxPackets = 0;
        }

        public void PrintStats()
        {
            Console.WriteLine("{0} RX: {1} bytes {2} packets", Device?.PciAddress, RxBytes, RxPackets);
            Console.WriteLine("{0} TX: {1} bytes {2} packets", Device?.PciAddress, TxBytes, TxPackets);
        }

        public void PrintStatsDiff(ref DeviceStats other, ulong nanos)
        {
            Console.WriteLine("{0} RX: {1} Mbit/s {2} Mpps",
                this.Device?.PciAddress,
                DiffMbit(this.RxBytes, other.RxBytes, this.RxPackets, other.RxPackets, nanos),
                DiffMpps(this.RxPackets, other.RxPackets, nanos)
            );
            Console.WriteLine("{0} TX: {1} Mbit/s {2} Mpps",
                this.Device?.PciAddress,
                DiffMbit(this.TxBytes, other.TxBytes, this.TxPackets, other.TxPackets, nanos),
                DiffMpps(this.TxPackets, other.TxPackets, nanos)
            );
        }
    }
}
