using System;
using System.Runtime.InteropServices;

namespace IxyCs.Ixgbe
{
    /*
        Another wrapper class which manages an actual RX descriptor living in DMA memory
        with a very specific data layout. Any reads/writes to this object
        are performed on the real descriptor

        The C implementation of the descriptor is shown at the bottom of the file for reference
     */
    public class IxgbeAdvRxDescriptor
    {
        public const int DescriptorSize = 16;

        //TODO: The C version says something about little endian for all these types
        //TODO: Marshal doesn't offer functions for uints for some reason. Converting to int is hopefully fine

        //read.pkt_addr - len: 8 - offs: 0
        public IntPtr PacketBufferAddress
        {
            get {return Marshal.ReadIntPtr(_baseAddress);}
            set {Marshal.WriteIntPtr(_baseAddress,0,value);}
        }

        //read.hdr_addr - len: 8 - offs: 8
        public IntPtr HeaderBufferAddress
        {
            get {return Marshal.ReadIntPtr(_baseAddress, 8);}
            set {Marshal.WriteIntPtr(_baseAddress, 8, value);}
        }

        //wb.lower.lo_dword.data - len: 4 - offs 0
        public uint WbData
        {
            get {return (uint)Marshal.ReadInt32(_baseAddress,0);}
            set {Marshal.WriteInt32(_baseAddress,0,(int)value);}
        }

        //wb.upper.status_error - len: 4 - offs 8
        public uint WbStatusError
        {
            get {return (uint)Marshal.ReadInt32(_baseAddress, 8);}
            set {Marshal.WriteInt32(_baseAddress, 8, (int)value);}
        }

        //wb.upper.length - len: 2 - offs: 12
        public ushort WbLength
        {
            get {return (ushort)Marshal.ReadInt16(_baseAddress, 12);}
            set {Marshal.WriteInt16(_baseAddress, 12, (short)value);}
        }

        //TODO: Some more fields..



        private IntPtr _baseAddress;

        public IxgbeAdvRxDescriptor(IntPtr baseAddr)
        {
            this._baseAddress = baseAddr;
        }
    }
}

/*
    union ixgbe_adv_rx_desc {
        struct {
            __le64 pkt_addr;  Packet buffer address
            __le64 hdr_addr;  Header buffer address
        } read;
        struct {
            struct {
                union {
                    __le32 data;
                    struct {
                        __le16 pkt_info;  RSS, Pkt type
                        __le16 hdr_info;  Splithdr, hdrlen
                    } hs_rss;
                } lo_dword;
                union {
                    __le32 rss;  RSS Hash
                    struct {
                        __le16 ip_id;  IP id
                        __le16 csum;  Packet Checksum
                    } csum_ip;
                } hi_dword;
            } lower;
            struct {
                __le32 status_error;  ext status/error
                __le16 length;  Packet length
                __le16 vlan;  VLAN tag
            } upper;
        } wb;   writeback
    };

*/