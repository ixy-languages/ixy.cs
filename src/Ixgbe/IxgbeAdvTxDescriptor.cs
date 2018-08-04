using System;

namespace IxyCs.Ixgbe
{
    public struct IxgbeAdvTxDescriptor
    {
        public const uint DescriptorSize = 16;
        private ulong _baseAddress;

        //read.buffer_addr - len: 8 - offs: 0
        public unsafe ulong BufferAddr
        {
            get
            {
                ulong *ptr = (ulong*)_baseAddress;
                return *ptr;
            }
            set
            {
                ulong *ptr = (ulong*)_baseAddress;
                *ptr = value;
            }
        }

        //read.cmd_type_len - len: 4 - offs: 8
        public unsafe uint CmdTypeLength
        {
            get
            {
                uint *ptr = (uint*)(_baseAddress + 8);
                return *ptr;
            }
            set
            {
                uint *ptr = (uint*)(_baseAddress + 8);
                *ptr = value;
            }
        }

        //read.olinfo_status - len: 4 - offs: 12
        public unsafe uint OlInfoStatus
        {
            get
            {
                uint *ptr = (uint*)(_baseAddress + 12);
                return *ptr;
            }
            set
            {
                uint *ptr = (uint*)(_baseAddress + 12);
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
        public bool IsNull {get {return _baseAddress == 0; }}

        public static IxgbeAdvTxDescriptor Null {get {return new IxgbeAdvTxDescriptor(0);}}

        public IxgbeAdvTxDescriptor(ulong baseAddr)
        {
            this._baseAddress = baseAddr;
        }
    }
}