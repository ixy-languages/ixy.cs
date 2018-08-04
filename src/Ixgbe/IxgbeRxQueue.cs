using System;
using IxyCs.Memory;

namespace IxyCs.Ixgbe
{
    public class IxgbeRxQueue : IxyQueue
    {
        public ulong[] VirtualAddresses;
        public ulong DescriptorsAddr {get; set;}
        public Mempool Mempool {get; set;}

        public IxgbeRxQueue(int entriesCount)
            :base(entriesCount)
        {
            this.VirtualAddresses = new ulong[entriesCount];
            DescriptorsAddr = 0;
        }

        /// <summary>
        /// Gets the Rx descriptor at index i, starting at DescriptorsAddr
        /// </summary>
        public IxgbeAdvRxDescriptor GetDescriptor(uint i)
        {
            if(DescriptorsAddr == 0)
                return IxgbeAdvRxDescriptor.Null;
            return new IxgbeAdvRxDescriptor(DescriptorsAddr + i * IxgbeAdvRxDescriptor.DescriptorSize);
        }

    }
}