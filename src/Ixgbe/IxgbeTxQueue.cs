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

        public unsafe void WriteCmdTypeLength(ushort descIndex, uint cmdTypeLen)
        {
            uint *ptr = (uint*)(GetDescriptor(descIndex) + 8);
            *ptr = cmdTypeLen;
        }

        public unsafe uint ReadCmdTypeLength(ushort descIndex)
        {
            uint *ptr = (uint*)(GetDescriptor(descIndex) + 8);
            return *ptr;
        }

        public unsafe void WriteOlInfoStatus(ushort descIndex, uint olInfoStat)
        {
            uint *ptr = (uint*)(GetDescriptor(descIndex) + 12);
            *ptr = olInfoStat;
        }

        public unsafe uint ReadOlInfoStatus(ushort descIndex)
        {
            uint *ptr = (uint*)(GetDescriptor(descIndex) + 12);
            return *ptr;
        }

        public unsafe void WriteWbStatus(ushort descIndex, uint wbStat)
        {
            uint *ptr = (uint*)(GetDescriptor(descIndex) + 12);
            *ptr = wbStat;
        }

        public unsafe uint ReadWbStatus(ushort descIndex)
        {
            uint *ptr = (uint*)(GetDescriptor(descIndex) + 12);
            return *ptr;
        }
    }
}