using System;
using System.IO;
using System.Runtime.InteropServices;

namespace IxyCs.Memory
{
    /*
        This class does not contain the actual packet buffer but is actually
        a wrapper containing the real buffer's address
        The reason for this is that the real buffer lives in DMA memory which is written to by
        the device and requires a very specific memory layout
     */
    public class PacketBuffer
    {
        public const int DataOffset = 64;
        //These buffers have 64 bytes of headroom so the actual data has an offset of 64 bytes
        /*
        Fields:
        0 - pointer Physical Address (64)
        64 - pointer mempool (64)
        128 - uint mempool index (32)
        160 - uint size (32)
        192 - byte[] more headroom (40 * 8)
        == 64 bytes
         */
        private IntPtr _baseAddress;
        private uint _size;

        //Physical Address, 64 bits, offset 0
        public IntPtr PhysicalAddress
        {
            get {return Marshal.ReadIntPtr(_baseAddress); }
            set {Marshal.WriteIntPtr(_baseAddress, 0, value);}
        }

        //TODO : Reference to mempool isn't possible, so we'll probably resort to some ID system

        //Mempool index, 32 bits, offset 128 bits
        public int MempoolIndex
        {
            get {return Marshal.ReadInt32(_baseAddress, 16);}
            set {Marshal.WriteInt32(_baseAddress, 16, value);}
        }

        //Size, 32 bits, offset 160 bits
        public int Size
        {
            get {return Marshal.ReadInt32(_baseAddress, 20);}
            set {Marshal.WriteInt32(_baseAddress, 20, value);}
        }

        public PacketBuffer(IntPtr baseAddr)
        {
            this._baseAddress = baseAddr;
        }
    }
}