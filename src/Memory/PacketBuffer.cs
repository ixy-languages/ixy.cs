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
    public unsafe struct PacketBuffer
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
        private long _baseAddress;
        //Confusingly, * is left-binding, not right-binding in C# so these are all pointers
        private long* _physicalAddress, _mempoolId;
        private uint* _mempoolIndex, _size;

        /// <summary>
        /// The virtual address of the actual Packet Buffer that this object wraps
        /// </summary>
        public long VirtualAddress{ get {return _baseAddress;} }

        /// <summary>
        /// If true, this buffer is not (successfully) initialized
        /// </summary>
        public bool IsNull {get {return VirtualAddress == 0; }}

        public static PacketBuffer Null {get {return new PacketBuffer(0);}}

        //Physical Address, 64 bits, offset 0
        public unsafe long PhysicalAddress
        {
            get
            {
                return *_physicalAddress;
            }
            set
            {
                *_physicalAddress = value;
            }
        }

        //This id is 64 bits to keep the data as similar to the C version as possible
        public unsafe long MempoolId
        {
            get
            {
                return *_mempoolId;
            }
            set
            {
                *_mempoolId = value;
            }
        }

        //Mempool index, 32 bits, offset 128 bits
        public unsafe uint MempoolIndex
        {
            get
            {
                return *_mempoolIndex;
            }
            set
            {
                *_mempoolIndex = value;
            }
        }

        //Size, 32 bits, offset 160 bits
        public unsafe uint Size
        {
            get
            {
                return *_size;
            }
            set
            {
                *_size = value;
            }
        }

        public PacketBuffer(long baseAddr)
        {
            this._baseAddress = baseAddr;
            _physicalAddress = (long*)_baseAddress;
            _mempoolId = (long*)(_baseAddress + 8);
            _mempoolIndex = (uint*)(_baseAddress + 16);
            _size = (uint*)(_baseAddress + 20);
        }

        //Sacrificing some code compactness for a nicer API
        //TODO: These functions should probably check if the offset is within bounds
        //but that would require checking buffer size with each call and impact performance
        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(int offset, int val)
        {
            int *ptr = (int*)(_baseAddress + DataOffset + offset);
            *ptr = val;
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(int offset, short val)
        {
            short *ptr = (short*)(_baseAddress + DataOffset + offset);
            *ptr = val;
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(int offset, IntPtr val)
        {
            IntPtr *ptr = (IntPtr*)(_baseAddress + DataOffset + offset);
            *ptr = val;
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(int offset, long val)
        {
            long *ptr = (long*)(_baseAddress + DataOffset + offset);
            *ptr = val;
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(int offset, byte val)
        {
            byte *ptr = (byte*)(_baseAddress + DataOffset + offset);
            *ptr = val;
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(int offset, byte[] val)
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

        /// <summary>
        /// Returns a copy of the buffer's payload
        /// </summary>
        public byte[] CopyData()
        {
            return CopyData(0, Size);
        }

        public byte[] CopyData(uint offset, uint length)
        {
            var cpy = new byte[length];
            Marshal.Copy(new IntPtr(_baseAddress + DataOffset + (int)offset), cpy, 0, cpy.Length);
            return cpy;
        }
    }
}