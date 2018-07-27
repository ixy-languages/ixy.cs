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
        public const int DescriptorSize = 16;

        //read.pkt_addr - len: 8 - offs: 0
        public unsafe IntPtr PacketBufferAddress
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

        //read.hdr_addr - len: 8 - offs: 8
        public unsafe IntPtr HeaderBufferAddress
        {
            get
            {
                IntPtr *ptr = (IntPtr*)IntPtr.Add(_baseAddress, 8);
                return *ptr;
            }
            set
            {
                IntPtr *ptr = (IntPtr*)IntPtr.Add(_baseAddress, 8);
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
                uint *ptr = (uint*)IntPtr.Add(_baseAddress, 8);
                return *ptr;
            }
            set
            {
                uint *ptr = (uint*)IntPtr.Add(_baseAddress, 8);
                *ptr = value;
            }
        }

        //wb.upper.length - len: 2 - offs: 12
        public unsafe ushort WbLength
        {
            get
            {
                ushort *ptr = (ushort*)IntPtr.Add(_baseAddress, 12);
                return *ptr;
            }
            set
            {
                ushort *ptr = (ushort*)IntPtr.Add(_baseAddress, 12);
                *ptr = value;
            }
        }

        //TODO: Some more fields..



        private IntPtr _baseAddress;

        /// <summary>
        /// If true, this descriptor is not (successfully) initialized
        /// </summary>
        public bool IsNull {get {return _baseAddress == IntPtr.Zero; }}

        public static IxgbeAdvRxDescriptor Null {get {return new IxgbeAdvRxDescriptor(IntPtr.Zero);}}

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