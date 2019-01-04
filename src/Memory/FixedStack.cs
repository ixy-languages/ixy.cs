using System.Collections;

namespace IxyCs.Memory
{
    /// <summary>
    /// Very fast fixed-size stack implementation for PacketBuffers. Could easily be turned into a generic stack
    /// Bounds checking is delegated to Mempool or application. Fairly unsafe solution,
    /// but this does provide a large performance increase and as this driver is not meant for production
    /// environments, the trade-off is acceptable.
    /// </summary>
    public struct FixedStack
    {
        private PacketBuffer[] _buffers;
        private int _top;
        private uint _size;

        public int Count {get {return _top + 1; }}
        public uint Capacity {get {return _size; }}
        public int Free {get {return (int)(_size - _top - 1);}}

        public FixedStack(uint size)
        {
            _size = size;
            _top = -1;
            _buffers = new PacketBuffer[_size];
        }

        public void Push(PacketBuffer pb)
        {
            _buffers[++_top] = pb;
        }

        public PacketBuffer Pop()
        {
            return _buffers[_top--];
        }
    }
}