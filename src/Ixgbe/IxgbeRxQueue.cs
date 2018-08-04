using System;
using IxyCs.Memory;

namespace IxyCs.Ixgbe
{
    public class IxgbeRxQueue : IxyQueue
    {
        private const uint DescriptorSize = 16;

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
        public ulong GetDescriptor(ushort i)
        {
            if(DescriptorsAddr == 0)
                throw new InvalidOperationException("Trying to retreive descriptor from uninitialized TX queue");
            return DescriptorsAddr + i * DescriptorSize;
        }

        public unsafe void WriteBufferAddress(ushort descIndex, ulong bufAddr)
        {
            ulong *ptr = (ulong*)GetDescriptor(descIndex);
            *ptr = bufAddr;
        }

        public unsafe ulong ReadBufferAddress(ushort descIndex)
        {
            ulong *ptr = (ulong*)GetDescriptor(descIndex);
            return *ptr;
        }

        public unsafe void WriteHeaderBufferAddress(ushort descIndex, ulong headBufAddr)
        {
            ulong *ptr = (ulong*)(GetDescriptor(descIndex) + 8);
            *ptr = headBufAddr;
        }

        public unsafe ulong ReadHeaderBufferAddress(ushort descIndex)
        {
            ulong *ptr = (ulong*)(GetDescriptor(descIndex) + 8);
            return *ptr;
        }

        public unsafe void WriteWbData(ushort descIndex, uint wbData)
        {
            uint *ptr = (uint*)GetDescriptor(descIndex);
            *ptr = wbData;
        }

        public unsafe ulong ReadWbData(ushort descIndex)
        {
            uint *ptr = (uint*)GetDescriptor(descIndex);
            return *ptr;
        }

        public unsafe void WriteWbStatusError(ushort descIndex, uint status)
        {
            uint *ptr = (uint*)(GetDescriptor(descIndex) + 8);
            *ptr = status;
        }

        public unsafe ulong ReadWbStatusError(ushort descIndex)
        {
            uint *ptr = (uint*)(GetDescriptor(descIndex) + 8);
            return *ptr;
        }

        public unsafe void WriteWbLength(ushort descIndex, ushort len)
        {
            ushort *ptr = (ushort*)(GetDescriptor(descIndex) + 12);
            *ptr = len;
        }

        public unsafe ushort ReadWbLength(ushort descIndex)
        {
            ushort *ptr = (ushort*)(GetDescriptor(descIndex) + 12);
            return *ptr;
        }

    }
}