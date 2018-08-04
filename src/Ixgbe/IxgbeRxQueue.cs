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
        public ulong GetDescriptorAddress(ushort i)
        {
            if(DescriptorsAddr == 0)
                throw new InvalidOperationException("Trying to retreive descriptor from uninitialized TX queue");
            return DescriptorsAddr + i * DescriptorSize;
        }

        public unsafe void WriteBufferAddress(ulong descAddr, ulong bufAddr)
        {
            ulong *ptr = (ulong*)descAddr;
            *ptr = bufAddr;
        }

        public unsafe ulong ReadBufferAddress(ulong descAddr)
        {
            ulong *ptr = (ulong*)descAddr;
            return *ptr;
        }

        public unsafe void WriteHeaderBufferAddress(ulong descAddr, ulong headBufAddr)
        {
            ulong *ptr = (ulong*)(descAddr + 8);
            *ptr = headBufAddr;
        }

        public unsafe ulong ReadHeaderBufferAddress(ulong descAddr)
        {
            ulong *ptr = (ulong*)(descAddr + 8);
            return *ptr;
        }

        public unsafe void WriteWbData(ulong descAddr, uint wbData)
        {
            uint *ptr = (uint*)descAddr;
            *ptr = wbData;
        }

        public unsafe ulong ReadWbData(ulong descAddr)
        {
            uint *ptr = (uint*)descAddr;
            return *ptr;
        }

        public unsafe void WriteWbStatusError(ulong descAddr, uint status)
        {
            uint *ptr = (uint*)(descAddr + 8);
            *ptr = status;
        }

        public unsafe ulong ReadWbStatusError(ulong descAddr)
        {
            uint *ptr = (uint*)(descAddr + 8);
            return *ptr;
        }

        public unsafe void WriteWbLength(ulong descAddr, ushort len)
        {
            ushort *ptr = (ushort*)(descAddr + 12);
            *ptr = len;
        }

        public unsafe ushort ReadWbLength(ulong descAddr)
        {
            ushort *ptr = (ushort*)(descAddr + 12);
            return *ptr;
        }

    }
}