using System;

namespace IxyCs.Ixgbe
{
    public class IxgbeTxQueue : IxyQueue
    {
        public long[] VirtualAddresses;
        public long DescriptorsAddr {get; set;}
        public ushort CleanIndex {get; set;}


        public IxgbeTxQueue(int entriesCount)
            :base(entriesCount)
        {
            this.VirtualAddresses = new long[entriesCount];
            DescriptorsAddr = 0;
            CleanIndex = 0;
        }

        /// <summary>
        /// Gets the Tx descriptor at index i, starting at DescriptorsAddr
        /// </summary>
        public IxgbeAdvTxDescriptor GetDescriptor(int i)
        {
            if(DescriptorsAddr == 0)
                return IxgbeAdvTxDescriptor.Null;
            return new IxgbeAdvTxDescriptor(DescriptorsAddr + i * IxgbeAdvTxDescriptor.DescriptorSize);
        }
    }
}