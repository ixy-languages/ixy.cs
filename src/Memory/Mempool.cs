using System;

namespace IxyCs.Memory
{
    public class Mempool
    {
        public readonly IntPtr BaseAddress;
        public readonly uint BufferSize, NumEntries;
        public uint[] Entries;

        public Mempool(IntPtr baseAddr, uint bufSize, uint numEntries)
        {
            this.BaseAddress = baseAddr;
            this.BufferSize = bufSize;
            this.NumEntries = numEntries;
            Entries = new uint[numEntries];
        }
    }
}