using System;

namespace IxyCs.Ixgbe
{
    public class IxgbeTxQueue : IxyQueue
    {
        public ulong[] VirtualAddresses;
        public ulong DescriptorsAddr {get; set;}
        public ushort CleanIndex {get; set;}


        public IxgbeTxQueue(int entriesCount)
            :base(entriesCount)
        {
            this.VirtualAddresses = new ulong[entriesCount];
            DescriptorsAddr = 0;
            CleanIndex = 0;
        }

        /// <summary>
        /// Gets the Tx descriptor at index i, starting at DescriptorsAddr
        /// </summary>
        public IxgbeAdvTxDescriptor GetDescriptor(uint i)
        {
            if(DescriptorsAddr == 0)
                return IxgbeAdvTxDescriptor.Null;
            return new IxgbeAdvTxDescriptor(DescriptorsAddr + i * IxgbeAdvTxDescriptor.DescriptorSize);
        }
    }
}