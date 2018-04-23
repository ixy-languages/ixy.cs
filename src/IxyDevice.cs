using System;
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

        /// <summary>
        /// Gets / Sets the promisc status. This should only be set in the SetPromisc method
        /// </summary>
        /// <returns></returns>
        public bool PromiscEnabled{get; protected set;}

        /// <summary>
        /// The file that the PCI device's memory is mapped to. Replaces the C version's mmap.
        /// On setting this, a MemoryMappedFileAccess  (MemMapAccess) is automatically created for the entire file
        /// </summary>
        protected MemoryMappedFile MemMap
        {
            get {return _memMap; }
            set
            {
                _memMap?.Dispose();
                _memMap = value;
                MemMapAccess?.Dispose();
                //Create accessor for entire file
                MemMapAccess = _memMap.CreateViewAccessor(0, 0);
            }

        }
        protected MemoryMappedViewAccessor MemMapAccess {get; private set;}

        private MemoryMappedFile _memMap;

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
                    Log.Error("Could not read config file for device with given PCI address");
                    Environment.Exit(1);
                }
                else
                    throw ex;
            }

            if(classId != 2)
            {
                Log.Error("Device {0} is not an NIC", pciAddr);
                Environment.Exit(1);
            }

            if(vendorId == 0x1af4 && deviceId >= 0x1000)
            {
                Log.Error("Virtio driver is currently not implemented");
                Environment.Exit(1);
            }

            //Now the inherited class (i.e. IxgbeDevice) will perform driver-specific initialization
        }

        public void Dispose()
        {
            MemMap?.Dispose();
            MemMapAccess?.Dispose();
        }

        public abstract uint RxBatch(int queueId, PacketBuffer[] buffers);
        public abstract uint TxBatch(int queueId, PacketBuffer[] buffers);
        public abstract void ReadStats(ref DeviceStats stats);
        public abstract uint GetLinkSpeed();
        public abstract void SetPromisc(bool enabled);

        //Some functions for reading/modifying mapped pci data
        //These might need memory barriers
        protected void SetReg(uint offset, uint value)
        {
            MemMapAccess.Write(offset, value);
        }

        protected uint GetReg(uint offset)
        {
            return MemMapAccess.ReadUInt32(offset);
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
                mask.ToString("X8"), offset.ToString("X("), current.ToString("X4"));
                //0.01 seconds
                Thread.Sleep(10);
                current = GetReg(offset);
            }
        }

        protected void WaitSetReg32(uint offset, uint mask)
        {
            uint current = GetReg(offset);
            while((current & mask) != mask)
            {
                Log.Notice("Waiting for flags 0x{0} in register 0x{1} to clear, current value 0x{2}",
                mask.ToString("X8"), offset.ToString("X("), current.ToString("X4"));
                Thread.Sleep(10);
                current = GetReg(offset);
            }
        }
    }
}