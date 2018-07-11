using System;
using System.Collections.Generic;

namespace IxyCs.Memory
{
    public class Mempool
    {
        //TODO : Pool management can be cleaned up a little
        //Static mempool management - this is necessary because PacketBuffers need to reference
        //mempools and we can't save references to managed memory in DMA
        public static readonly List<Mempool> Pools = new List<Mempool>();
        public static Mempool FindPool(long id) {return Pools.Find(pool => pool.Id == id);}

        public static void FreePool(Mempool pool) {Pools.Remove(pool);}
        public static void AddPool(Mempool pool)
        {
            long i = 0;
            while(!ValidId(i))
                i++;
            pool.Id = i;
            Pools.Add(pool);
        }
        private static bool ValidId(long id) {return FindPool(id) == null;}


        public readonly IntPtr BaseAddress;
        public readonly uint BufferSize, NumEntries;
        /// <summary>
        /// Is used to identify the mempool so that PacketBuffers can reference them
        /// </summary>
        public long Id
        {
            get {return _id;}
            set
            {
                if(ValidId(value))
                    _id = value;
                else
                    throw new ArgumentException("This mempool id is already in use");
            }
        }
        public uint[] Entries;
        public uint FreeStackTop {get; private set;}

        private long _id;

        public Mempool(IntPtr baseAddr, uint bufSize, uint numEntries)
        {
            this.BaseAddress = baseAddr;
            this.BufferSize = bufSize;
            this.NumEntries = numEntries;
            this.FreeStackTop = numEntries;
            this.Entries = new uint[numEntries];
            //Register this mempool to static list of pools
            Mempool.AddPool(this);
        }

        public PacketBuffer AllocatePacketBuffer()
        {
            if(FreeStackTop < 1)
            {
                Log.Warning("Memory pool is out of free buffers - ignoring request for allocation");
                return null;
            }

            uint entryId = Entries[--FreeStackTop];
            var virtAddr = IntPtr.Add(BaseAddress, (int)(entryId * BufferSize));
            var buffer = new PacketBuffer(virtAddr);
            buffer.MempoolId = Id;
            return new PacketBuffer(virtAddr);
        }

        public PacketBuffer[] AllocatePacketBuffers(int num)
        {
            if(FreeStackTop < num)
            {
                Log.Warning("Mempool only has {0} free buffers, requested {1}", FreeStackTop, num);
                num = (int)FreeStackTop;
            }
            var buffers = new PacketBuffer[num];
            for(int i = 0; i < num; i++)
            {
                buffers[i] = AllocatePacketBuffer();
            }
            return buffers;
        }

        public void FreeBuffer(PacketBuffer buffer)
        {
            Entries[FreeStackTop++] = (uint)buffer.MempoolIndex;
        }
    }
}