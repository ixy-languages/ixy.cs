using System;
using System.Runtime.InteropServices;

namespace IxyCs.Ixgbe
{
    public class IxgbeAdvTxDescriptor
    {
        public const int DescriptorSize = 16;
        private IntPtr _baseAddr;

        //read.buffer_addr - len: 8 - offs: 0
        public IntPtr BufferAddr
        {
            get {return Marshal.ReadIntPtr(_baseAddr, 0);}
            set {Marshal.WriteIntPtr(_baseAddr, 0, value);}
        }

        //read.cmd_type_len - len: 4 - offs: 8
        public uint CmdTypeLength
        {
            get {return (uint)Marshal.ReadInt32(_baseAddr, 8);}
            set {Marshal.WriteInt32(_baseAddr, 8, (int)value);}
        }

        //read.olinfo_status - len: 4 - offs: 12
        public uint OlInfoStatus
        {
            get {return (uint)Marshal.ReadInt32(_baseAddr, 12);}
            set {Marshal.WriteInt32(_baseAddr, 12, (int)value);}
        }

        //wb.status - len: 4 - offs: 12
        public uint WbStatus
        {
            get {return (uint)Marshal.ReadInt32(_baseAddr, 12);}
            set {Marshal.WriteInt32(_baseAddr, 12, (int)value);}
        }

        public IxgbeAdvTxDescriptor(IntPtr baseAddr)
        {
            this._baseAddr = baseAddr;
        }
    }
}