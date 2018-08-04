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
    public struct IxgbeAdvRxDescriptor
    {
        public const uint DescriptorSize = 16;

        //read.pkt_addr - len: 8 - offs: 0
        public unsafe ulong PacketBufferAddress
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

        //read.hdr_addr - len: 8 - offs: 8
        public unsafe ulong HeaderBufferAddress
        {
            get
            {
                ulong *ptr = (ulong*)(_baseAddress + 8);
                return *ptr;
            }
            set
            {
                ulong *ptr = (ulong*)(_baseAddress + 8);
                *ptr = value;
            }
        }

        //wb.lower.lo_dword.data - len: 4 - offs 0
        public unsafe uint WbData
        {
           get
            {
                uint *ptr = (uint*)_baseAddress;
                return *ptr;
            }
            set
            {
                uint *ptr = (uint*)_baseAddress;
                *ptr = value;
            }
        }

        //wb.upper.status_error - len: 4 - offs 8
        public unsafe uint WbStatusError
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

        //wb.upper.length - len: 2 - offs: 12
        public unsafe ushort WbLength
        {
            get
            {
                ushort *ptr = (ushort*)(_baseAddress + 12);
                return *ptr;
            }
            set
            {
                ushort *ptr = (ushort*)(_baseAddress + 12);
                *ptr = value;
            }
        }

        //There are some more fields here but we don't need them

        private ulong _baseAddress;

        /// <summary>
        /// If true, this descriptor is not (successfully) initialized
        /// </summary>
        public bool IsNull {get {return _baseAddress == 0; }}

        public static IxgbeAdvRxDescriptor Null {get {return new IxgbeAdvRxDescriptor(0);}}

        public IxgbeAdvRxDescriptor(ulong baseAddr)
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