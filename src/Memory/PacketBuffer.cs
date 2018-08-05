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
        public const uint DataOffset = 64;
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
        private UnmanagedMemoryStream _baseStream;
        private ulong _virtualAddress;

        /// <summary>
        /// The virtual address of the actual Packet Buffer that this object wraps
        /// </summary>
        public ulong VirtualAddress{ get {return _virtualAddress;} }

        /// <summary>
        /// If true, this buffer is not (successfully) initialized
        /// </summary>
        public bool IsNull {get {return VirtualAddress == 0; }}

        public static PacketBuffer Null {get {return new PacketBuffer(0);}}

        //Physical Address, 64 bits, offset 0
        public unsafe ulong PhysicalAddress
        {
            get
            {
                _baseStream.Seek(0, SeekOrigin.Begin);
                var bytes = new byte[8];
                _baseStream.Read(bytes, 0, 8);
                return BitConverter.ToUInt64(bytes, 0);
            }
            set
            {
                _baseStream.Seek(0, SeekOrigin.Begin);
                _baseStream.Write(BitConverter.GetBytes(value), 0, 8);
            }
        }

        //This id is 64 bits to keep the data as similar to the C version as possible
        public unsafe long MempoolId
        {
            get
            {
                _baseStream.Seek(8, SeekOrigin.Begin);
                var bytes = new byte[8];
                _baseStream.Read(bytes, 0, 8);
                return BitConverter.ToInt64(bytes, 0);
            }
            set
            {
                _baseStream.Seek(8, SeekOrigin.Begin);
                _baseStream.Write(BitConverter.GetBytes(value), 0, 8);
            }
        }

        //Size, 32 bits, offset 160 bits
        public unsafe uint Size
        {
            get
            {
                _baseStream.Seek(20, SeekOrigin.Begin);
                var bytes = new byte[4];
                _baseStream.Read(bytes, 0, 4);
                return BitConverter.ToUInt32(bytes, 0);
            }
            set
            {
                _baseStream.Seek(20, SeekOrigin.Begin);
                _baseStream.Write(BitConverter.GetBytes(value), 0, 4);
            }
        }

        public unsafe PacketBuffer(ulong baseAddr, uint maxSize)
        {
            _virtualAddress = baseAddr;
            var ptr = (byte*)baseAddr;
            _baseStream = new UnmanagedMemoryStream(ptr, maxSize, maxSize, FileAccess.ReadWrite);
        }

        //Private constructor only to be used for null-initialization
        private PacketBuffer(ulong baseAddr)
        {
            _virtualAddress = baseAddr;
            _baseStream = null;
        }

        //Sacrificing some code compactness for a nicer API
        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(uint offset, int val)
        {
            _baseStream.Seek(DataOffset + offset, SeekOrigin.Begin);
            _baseStream.Write(BitConverter.GetBytes(val), 0 , 4);
        }
        /*
        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(uint offset, short val)
        {
            short *ptr = (short*)(_baseAddress + DataOffset + offset);
            *ptr = val;
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(uint offset, IntPtr val)
        {
            IntPtr *ptr = (IntPtr*)(_baseAddress + DataOffset + offset);
            *ptr = val;
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(uint offset, long val)
        {
            long *ptr = (long*)(_baseAddress + DataOffset + offset);
            *ptr = val;
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(uint offset, byte val)
        {
            byte *ptr = (byte*)(_baseAddress + DataOffset + offset);
            *ptr = val;
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(uint offset, byte[] val)
        {
            if(val == null || val.Length == 0)
                return;
            byte *targetPtr = (byte*)(_baseAddress + DataOffset + offset);
            //Keep location of source array in place while copying data
            fixed(byte* sourcePtr = val)
            {
                for(int i = 0; i < val.Length; i++)
                    targetPtr[i] = sourcePtr[i];
            }
        }
        */

        /// <summary>
        /// Returns one byte of data at the given offset. Used for debug/benchmark purposes
        /// </summary>
        public unsafe byte GetDataByte(uint i)
        {
            _baseStream.Seek(i + DataOffset, SeekOrigin.Begin);
            return (byte)_baseStream.ReadByte();
        }


        /// <summary>
        /// Returns a copy of the buffer's payload
        /// </summary>
        public byte[] CopyData()
        {
            return CopyData(0, Size);
        }

        public byte[] CopyData(uint offset, uint length)
        {
            _baseStream.Seek(offset + DataOffset, SeekOrigin.Begin);
            var cpy = new byte[length];
            _baseStream.Read(cpy, 0, (int)length);
            return cpy;
        }
    }
}