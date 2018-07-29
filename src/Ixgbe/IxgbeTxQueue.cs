using System;

namespace IxyCs.Ixgbe
{
    public class IxgbeTxQueue : IxyQueue
    {
        public IntPtr[] VirtualAddresses;
        public IntPtr DescriptorsAddr {get; set;}
        public ushort CleanIndex {get; set;}


        public IxgbeTxQueue(int entriesCount)
            :base(entriesCount)
        {
            this.VirtualAddresses = new IntPtr[entriesCount];
            DescriptorsAddr = IntPtr.Zero;
            CleanIndex = 0;
        }

        /// <summary>
        /// Gets the Tx descriptor at index i, starting at DescriptorsAddr
        /// </summary>
        public IxgbeAdvTxDescriptor GetDescriptor(int i)
        {
            if(DescriptorsAddr == IntPtr.Zero)
                return IxgbeAdvTxDescriptor.Null;
            return new IxgbeAdvTxDescriptor(IntPtr.Add(DescriptorsAddr, i * IxgbeAdvTxDescriptor.DescriptorSize));
        }
    }
}