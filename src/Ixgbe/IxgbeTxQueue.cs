using System;

namespace IxyCs.Ixgbe
{
    public class IxgbeTxQueue : IxyQueue
    {
        public IntPtr[] VirtualAddresses;
        public IntPtr DescriptorsAddr {get; set;}
        public int CleanIndex {get; set;}


        public IxgbeTxQueue(int entriesCount)
            :base(entriesCount)
        {
            this.VirtualAddresses = new IntPtr[entriesCount];
            DescriptorsAddr = IntPtr.Zero;
            CleanIndex = 0;
        }
    }
}