using System;
using System.Runtime.InteropServices;

namespace IxyCs.Ixgbe
{
    public struct IxgbeAdvTxDescriptor
    {
        public const int DescriptorSize = 16;
        private IntPtr _baseAddress;

        //read.buffer_addr - len: 8 - offs: 0
        public unsafe IntPtr BufferAddr
        {
            get
            {
                IntPtr *ptr = (IntPtr*)_baseAddress;
                return *ptr;
            }
            set
            {
                IntPtr *ptr = (IntPtr*)_baseAddress;
                *ptr = value;
            }
        }

        //read.cmd_type_len - len: 4 - offs: 8
        public unsafe uint CmdTypeLength
        {
            get
            {
                uint *ptr = (uint*)IntPtr.Add(_baseAddress, 8);
                return *ptr;
            }
            set
            {
                uint *ptr = (uint*)IntPtr.Add(_baseAddress, 8);
                *ptr = value;
            }
        }

        //read.olinfo_status - len: 4 - offs: 12
        public unsafe uint OlInfoStatus
        {
            get
            {
                uint *ptr = (uint*)IntPtr.Add(_baseAddress, 12);
                return *ptr;
            }
            set
            {
                uint *ptr = (uint*)IntPtr.Add(_baseAddress, 12);
                *ptr = value;
            }
        }

        //wb.status - len: 4 - offs: 12
        public unsafe uint WbStatus
        {
            get {return OlInfoStatus;}
            set {OlInfoStatus = value;}
        }

        /// <summary>
        /// If true, this descriptor is not (successfully) initialized
        /// </summary>
        public bool IsNull {get {return _baseAddress == IntPtr.Zero; }}

        public static IxgbeAdvTxDescriptor Null {get {return new IxgbeAdvTxDescriptor(IntPtr.Zero);}}

        public IxgbeAdvTxDescriptor(IntPtr baseAddr)
        {
            this._baseAddress = baseAddr;
        }
    }
}