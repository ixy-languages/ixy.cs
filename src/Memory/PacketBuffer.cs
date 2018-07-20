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
    public struct PacketBuffer
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

        /// <summary>
        /// The virtual address of the actual Packet Buffer that this object wraps
        /// </summary>
        public IntPtr VirtualAddress{ get {return _baseAddress;} }

        /// <summary>
        /// If true, this buffer is not (successfully) initialized
        /// </summary>
        public bool IsNull {get {return VirtualAddress == IntPtr.Zero; }}

        public static PacketBuffer Null {get {return new PacketBuffer(IntPtr.Zero);}}

        //Physical Address, 64 bits, offset 0
        public IntPtr PhysicalAddress
        {
            get {return Marshal.ReadIntPtr(_baseAddress); }
            set {Marshal.WriteIntPtr(_baseAddress, 0, value);}
        }

        //This id is 64 bits to keep the data as similar to the C version as possible
        public long MempoolId
        {
            get {return Marshal.ReadInt64(_baseAddress, 8);}
            set {Marshal.WriteInt64(_baseAddress, 8, value);}
        }

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

        //Sacrificing some code compactness for a nicer API

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public void WriteData(int offset, int val)
        {
            Marshal.WriteInt32(IntPtr.Add(VirtualAddress, DataOffset + offset), val);
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public void WriteData(int offset, short val)
        {
            Marshal.WriteInt16(IntPtr.Add(VirtualAddress, DataOffset + offset), val);
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public void WriteData(int offset, IntPtr val)
        {
            Marshal.WriteIntPtr(IntPtr.Add(VirtualAddress, DataOffset + offset), val);
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public void WriteData(int offset, long val)
        {
            Marshal.WriteInt64(IntPtr.Add(VirtualAddress, DataOffset + offset), val);
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public void WriteData(int offset, byte val)
        {
            Marshal.WriteByte(IntPtr.Add(VirtualAddress, DataOffset + offset), val);
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public void WriteData(int offset, byte[] val)
        {
            Marshal.Copy(val, 0, IntPtr.Add(VirtualAddress, DataOffset + offset), Math.Min(val.Length, Size - offset));
        }
    }
}