using System;
using IxyCs.Memory;

namespace IxyCs.Ixgbe
{
    public class IxgbeRxQueue : IxyQueue
    {
        public IntPtr[] VirtualAddresses;
        public IntPtr DescriptorsAddr {get; set;}
        public Mempool Mempool {get; set;}

        public IxgbeRxQueue(int entriesCount)
            :base(entriesCount)
        {
            this.VirtualAddresses = new IntPtr[entriesCount];
            DescriptorsAddr = IntPtr.Zero;
        }

        /// <summary>
        /// Gets the Rx descriptor at index i, starting at DescriptorsAddr
        /// </summary>
        public IxgbeAdvRxDescriptor GetDescriptor(int i)
        {
            if(DescriptorsAddr == IntPtr.Zero)
                return IxgbeAdvRxDescriptor.Null;
            //TODO TEST : Is pointer arithmetic correct here?
            return new IxgbeAdvRxDescriptor(IntPtr.Add(DescriptorsAddr, i * IxgbeAdvRxDescriptor.DescriptorSize));
        }

    }
}