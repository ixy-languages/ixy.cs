using System;
using System.Collections.Generic;

namespace IxyCs.Ixgbe
{
    public class IxgbeTxQueue : IxyQueue
    {
        public IntPtr[] VirtualAddresses;
        public IntPtr DescriptorsAddr {get; set;}
        public ushort CleanIndex {get; set;}

        //Pool of descriptors to avoid allocations
        private Stack<IxgbeAdvTxDescriptor> _descriptors;

        public IxgbeTxQueue(int entriesCount)
            :base(entriesCount)
        {
            this.VirtualAddresses = new IntPtr[entriesCount];
            DescriptorsAddr = IntPtr.Zero;
            CleanIndex = 0;
            _descriptors = new Stack<IxgbeAdvTxDescriptor>(EntriesCount);
            for(int i = 0; i < EntriesCount; i++)
                _descriptors.Push(IxgbeAdvTxDescriptor.Null);
        }

        /// <summary>
        /// Gets the Tx descriptor at index i, starting at DescriptorsAddr
        /// </summary>
        public IxgbeAdvTxDescriptor GetDescriptor(int i)
        {
            if(DescriptorsAddr == IntPtr.Zero)
                return IxgbeAdvTxDescriptor.Null;
            //TODO : Check pointer arithmetic
            IntPtr addr = IntPtr.Add(DescriptorsAddr, i * IxgbeAdvTxDescriptor.DescriptorSize);
            //Try to get a descriptor from the pool
            if(_descriptors.Count > 0)
            {
                var descriptor = _descriptors.Pop();
                descriptor.BaseAddress = addr;
                return descriptor;
            }
            //If pool is empty, allocate new descriptor
            return new IxgbeAdvTxDescriptor(addr);
        }

        /// <summary>
        /// Recycles descriptor object. Does not do anything to actual data of
        /// descriptor. Object's BaseAddr will be rewritten before reuse
        /// </summary>
        public void ReturnDescriptor(IxgbeAdvTxDescriptor descriptor)
        {
            _descriptors.Push(descriptor);
        }
    }
}