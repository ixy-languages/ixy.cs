using System;

namespace IxyCs.Ixgbe
{
    public class IxgbeRxQueue : IxyQueue
    {
        public IntPtr[] VirtualAddresses;
        public IntPtr DescriptorsAddr {get; set;}

        public IxgbeRxQueue(int entriesCount)
            :base(entriesCount)
        {
            this.VirtualAddresses = new IntPtr[entriesCount];
            DescriptorsAddr = IntPtr.Zero;
        }

    }
}