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
            unchecked
            {
                return DescriptorsAddr + i * DescriptorSize;
            }
        }

        public unsafe void WriteBufferAddress(ulong descAddr, ulong bufAddr)
        {
            *((ulong*)descAddr) = bufAddr;
        }

        public unsafe ulong ReadBufferAddress(ulong descAddr)
        {
            return *((ulong*)descAddr);
        }

        public unsafe void WriteHeaderBufferAddress(ulong descAddr, ulong headBufAddr)
        {
            *((ulong*)(unchecked(descAddr + 8))) = headBufAddr;
        }

        public unsafe ulong ReadHeaderBufferAddress(ulong descAddr)
        {
            return *((ulong*)(unchecked(descAddr + 8)));
        }

        public unsafe void WriteWbData(ulong descAddr, uint wbData)
        {
            *((uint*)descAddr) = wbData;
        }

        public unsafe ulong ReadWbData(ulong descAddr)
        {
            return *((uint*)descAddr);
        }

        public unsafe void WriteWbStatusError(ulong descAddr, uint status)
        {
            *((uint*)(unchecked(descAddr + 8))) = status;
        }

        public unsafe ulong ReadWbStatusError(ulong descAddr)
        {
            return *((uint*)(unchecked(descAddr + 8)));
        }

        public unsafe void WriteWbLength(ulong descAddr, ushort len)
        {
            *((ushort*)(unchecked(descAddr + 12))) = len;
        }

        public unsafe ushort ReadWbLength(ulong descAddr)
        {
            return *((ushort*)(unchecked(descAddr + 12)));
        }

    }
}