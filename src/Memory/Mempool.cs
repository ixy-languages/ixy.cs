using System;
using System.Collections.Generic;
using System.Linq;

namespace IxyCs.Memory
{
    public class Mempool
    {
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
        //---End of static management

        public readonly ulong BaseAddress;
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

        private long _id;
        //Pre-allocated buffer objects for this mempool
        private FixedStack _buffers;

        public Mempool(ulong baseAddr, uint bufSize, uint numEntries)
        {
            this.BaseAddress = baseAddr;
            this.BufferSize = bufSize;
            this.NumEntries = numEntries;
            //Register this mempool to static list of pools
            Mempool.AddPool(this);
        }

        public void PreallocateBuffers()
        {
            _buffers = new FixedStack(NumEntries);
            for(int i = (int)NumEntries - 1; i >= 0; i--)
            {
                var virtAddr = BaseAddress + (uint)i * BufferSize;
                var buffer = new PacketBuffer(virtAddr);
                buffer.MempoolId = Id;
                buffer.PhysicalAddress = MemoryHelper.VirtToPhys(virtAddr);
                buffer.Size = 0;
                _buffers.Push(buffer);
            }
        }

        public PacketBuffer GetPacketBuffer()
        {
            if(_buffers.Count < 1)
            {
                Log.Warning("Memory pool is out of free buffers - ignoring request for allocation");
                return PacketBuffer.Null;
            }
            return _buffers.Pop();
        }

        internal PacketBuffer GetPacketBufferFast()
        {
            return _buffers.Pop();
        }

        public int GetPacketBuffers(Span<PacketBuffer> buffers)
        {
            var num = buffers.Length;
            if (_buffers.Count < num)
            {
                Log.Warning("Mempool only has {0} free buffers, requested {1}", _buffers.Count, num);
                num = _buffers.Count;
            }
            for (int i = 0; i < num; i++)
                buffers[i] = _buffers.Pop();
            return num;
        }

        /// <summary>
        /// Returns the given buffer to the top of the mempool stack
        /// </summary>
        public void FreeBuffer(PacketBuffer buffer)
        {
            if(_buffers.Count < _buffers.Capacity)
                _buffers.Push(buffer);
            else
                Log.Warning("Cannot free buffer because mempool stack is full");
        }

        /// <summary>
        /// Fast version of FreeBuffer, which does not do any bounds checking. For internal use only!
        /// </summary>
        internal void FreeBufferFast(PacketBuffer buffer)
        {
            _buffers.Push(buffer);
        }
    }
}