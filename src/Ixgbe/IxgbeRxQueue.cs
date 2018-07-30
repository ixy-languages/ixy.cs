using System;
using IxyCs.Memory;

namespace IxyCs.Ixgbe
{
    public class IxgbeRxQueue : IxyQueue
    {
        public long[] VirtualAddresses;
        public long DescriptorsAddr {get; set;}
        public Mempool Mempool {get; set;}

        public IxgbeRxQueue(int entriesCount)
            :base(entriesCount)
        {
            this.VirtualAddresses = new long[entriesCount];
            DescriptorsAddr = 0;
        }

        /// <summary>
        /// Gets the Rx descriptor at index i, starting at DescriptorsAddr
        /// </summary>
        public IxgbeAdvRxDescriptor GetDescriptor(int i)
        {
            if(DescriptorsAddr == 0)
                return IxgbeAdvRxDescriptor.Null;
            return new IxgbeAdvRxDescriptor(DescriptorsAddr + i * IxgbeAdvRxDescriptor.DescriptorSize);
        }

    }
}