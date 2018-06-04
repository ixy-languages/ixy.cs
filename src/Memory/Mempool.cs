using System;

namespace IxyCs.Memory
{
    public class Mempool
    {
        public readonly IntPtr BaseAddress;
        public readonly uint BufferSize, NumEntries;
        public uint[] Entries;
        public uint FreeStackTop {get; private set;}

        public Mempool(IntPtr baseAddr, uint bufSize, uint numEntries)
        {
            this.BaseAddress = baseAddr;
            this.BufferSize = bufSize;
            this.NumEntries = numEntries;
            this.FreeStackTop = numEntries;
            this.Entries = new uint[numEntries];
        }

        public PacketBuffer AllocatePacketBuffer(out IntPtr virtAddr)
        {
            if(FreeStackTop < 1)
            {
                Log.Warning("Memory pool is out of free buffers - ignoring request for allocation");
                virtAddr = IntPtr.Zero;
                return null;
            }

            virtAddr = IntPtr.Add(BaseAddress, (int)(--FreeStackTop * BufferSize));
            return new PacketBuffer(virtAddr);
        }
    }
}