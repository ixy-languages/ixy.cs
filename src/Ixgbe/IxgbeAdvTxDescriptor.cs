using System;
using System.Runtime.InteropServices;

namespace IxyCs.Ixgbe
{
    public struct IxgbeAdvTxDescriptor
    {
        public const int DescriptorSize = 16;
        private IntPtr _baseAddress;

        public IntPtr BaseAddress
        {
            get { return _baseAddress;}
            //Should only be written by queue
            internal set {_baseAddress = value;}
        }

        //read.buffer_addr - len: 8 - offs: 0
        public IntPtr BufferAddr
        {
            get {return Marshal.ReadIntPtr(_baseAddress, 0);}
            set {Marshal.WriteIntPtr(_baseAddress, 0, value);}
        }

        //read.cmd_type_len - len: 4 - offs: 8
        public uint CmdTypeLength
        {
            get {return (uint)Marshal.ReadInt32(_baseAddress, 8);}
            set {Marshal.WriteInt32(_baseAddress, 8, (int)value);}
        }

        //read.olinfo_status - len: 4 - offs: 12
        public uint OlInfoStatus
        {
            get {return (uint)Marshal.ReadInt32(_baseAddress, 12);}
            set {Marshal.WriteInt32(_baseAddress, 12, (int)value);}
        }

        //wb.status - len: 4 - offs: 12
        public uint WbStatus
        {
            get {return (uint)Marshal.ReadInt32(_baseAddress, 12);}
            set {Marshal.WriteInt32(_baseAddress, 12, (int)value);}
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