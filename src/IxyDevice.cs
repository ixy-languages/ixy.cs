using System;
using System.Linq;
using System.IO.MemoryMappedFiles;
using System.Threading;
using IxyCs.Memory;
using IxyCs.Pci;

namespace IxyCs
{
    public abstract class IxyDevice : IDisposable
    {
        public string PciAddress{get; private set;}
        public string DriverName{get; protected set;}
        public IxyQueue[] RxQueues {get; protected set;}
        public IxyQueue[] TxQueues {get; protected set;}

        /// <summary>
        /// Gets / Sets the promisc status. This should only be set in the SetPromisc method
        /// </summary>
        /// <returns></returns>
        public bool PromiscEnabled{get; protected set;}

        /// <summary>
        /// The file that the PCI device's memory is mapped to. Replaces the C version's mmap.
        /// On setting this, a MemoryMappedFileAccess  (MemMapAccess) is automatically created for the entire file
        /// </summary>
        protected MemoryMappedFile PciMemMap
        {
            get {return _pciMemMap; }
            set
            {
                _pciMemMap?.Dispose();
                _pciMemMap = value;
                PciMemMapAccess?.Dispose();
                //Create accessor for entire file
                PciMemMapAccess = _pciMemMap.CreateViewAccessor(0, 0);
            }

        }
        protected MemoryMappedViewAccessor PciMemMapAccess {get; private set;}

        private MemoryMappedFile _pciMemMap;

        public IxyDevice(string pciAddr, int rxQueues, int txQueues)
        {
            this.PciAddress = pciAddr;
            ushort vendorId = 0;
            ushort deviceId = 0;
            uint classId = 0;
            try
            {
                using(var reader = new PciReader(pciAddr, "config"))
                {
                    vendorId = reader.Read16(0);
                    deviceId = reader.Read16(2);
                    classId = reader.Read32(8) >> 24;
                }
            } catch(Exception ex) {
                if(ex is System.IO.IOException || ex is InvalidOperationException)
                {
                    Log.Error("FATAL: Could not read config file for device with given PCI address - {0}", ex.Message);
                    Environment.Exit(1);
                }
                else
                    throw ex;
            }

            if(classId != 2)
            {
                Log.Error("FATAL: Device {0} is not an NIC", pciAddr);
                Environment.Exit(1);
            }

            if(vendorId == 0x1af4 && deviceId >= 0x1000)
            {
                Log.Error("FATAL: Virtio driver is currently not implemented");
                Environment.Exit(1);
            }

            //Now the inherited class (i.e. IxgbeDevice) will perform driver-specific initialization
        }

        public void Dispose()
        {
            PciMemMap?.Dispose();
            PciMemMapAccess?.Dispose();
        }

        /// <summary>
        /// Calls TxBatch until all packets are queued with busy waiting
        /// </summary>
        public void TxBatchBusyWait(int queueId, Span<PacketBuffer> buffers)
        {
            while(buffers.Length > 0)
            {
                var numSent = TxBatch(queueId, buffers);
                buffers = buffers.Slice(numSent);
            }
        }

        public abstract int RxBatch(int queueId, Span<PacketBuffer> buffers);
        public abstract int TxBatch(int queueId, Span<PacketBuffer> buffers);
        public abstract void ReadStats(ref DeviceStats stats);
        public abstract uint GetLinkSpeed();
        public abstract void SetPromisc(bool enabled);

        //Some functions for reading/modifying mapped pci data
        //These might need memory barriers
        protected void SetReg(uint offset, uint value)
        {
            PciMemMapAccess.Write(offset, value);
        }

        protected uint GetReg(uint offset)
        {
            return PciMemMapAccess.ReadUInt32(offset);
        }

        protected void SetFlags(uint offset, uint flags)
        {
            SetReg(offset, GetReg(offset) | flags);
        }

        protected void ClearFlags(uint offset, uint flags)
        {
            SetReg(offset, GetReg(offset) & ~flags);
        }

        protected void WaitClearReg(uint offset, uint mask)
        {
            uint current = GetReg(offset);
            while((current & mask) != 0)
            {
                Log.Notice("Waiting for flags 0x{0} in register 0x{1} to clear, current value 0x{2}",
                    mask.ToString("X"), offset.ToString("X"), current.ToString("X"));
                //0.01 seconds
                Thread.Sleep(10);
                current = GetReg(offset);
            }
        }

        protected void WaitSetReg(uint offset, uint mask)
        {
            uint current = GetReg(offset);
            while((current & mask) != mask)
            {
                Log.Notice("Waiting for flags 0x{0} in register 0x{1} to clear, current value 0x{2}",
                    mask.ToString("X"), offset.ToString("X"), current.ToString("X"));
                Thread.Sleep(10);
                current = GetReg(offset);
            }
        }
    }
}