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
            *((ulong*)descAddr) = bufAddr;
        }

        public unsafe ulong ReadBufferAddress(ulong descAddr)
        {
            return *((ulong*)descAddr);
        }

        public unsafe void WriteCmdTypeLength(ulong descAddr, uint cmdTypeLen)
        {
            *((uint*)(descAddr + 8)) = cmdTypeLen;
        }

        public unsafe uint ReadCmdTypeLength(ulong descAddr)
        {
            return *((uint*)(descAddr + 8));
        }

        public unsafe void WriteOlInfoStatus(ulong descAddr, uint olInfoStat)
        {
            *((uint*)(descAddr + 12)) = olInfoStat;
        }

        public unsafe uint ReadOlInfoStatus(ulong descAddr)
        {
            return *((uint*)(descAddr + 12));
        }

        public unsafe void WriteWbStatus(ulong descAddr, uint wbStat)
        {
            *((uint*)(descAddr + 12)) = wbStat;
        }

        public unsafe uint ReadWbStatus(ulong descAddr)
        {
            return *((uint*)(descAddr + 12));
        }
    }
}