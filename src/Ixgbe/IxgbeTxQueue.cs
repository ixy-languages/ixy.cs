using System;

namespace IxyCs.Ixgbe
{
    public class IxgbeTxQueue : IxyQueue
    {
        public const uint DescriptorSize = 16;

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

        public unsafe void WriteCmdTypeLength(ulong descAddr, uint cmdTypeLen)
        {
            uint *ptr = (uint*)(descAddr + 8);
            *ptr = cmdTypeLen;
        }

        public unsafe uint ReadCmdTypeLength(ulong descAddr)
        {
            uint *ptr = (uint*)(descAddr + 8);
            return *ptr;
        }

        public unsafe void WriteOlInfoStatus(ulong descAddr, uint olInfoStat)
        {
            uint *ptr = (uint*)(descAddr + 12);
            *ptr = olInfoStat;
        }

        public unsafe uint ReadOlInfoStatus(ulong descAddr)
        {
            uint *ptr = (uint*)(descAddr + 12);
            return *ptr;
        }

        public unsafe void WriteWbStatus(ulong descAddr, uint wbStat)
        {
            uint *ptr = (uint*)(descAddr + 12);
            *ptr = wbStat;
        }

        public unsafe uint ReadWbStatus(ulong descAddr)
        {
            uint *ptr = (uint*)(descAddr + 12);
            return *ptr;
        }
    }
}