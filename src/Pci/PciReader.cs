using System;
using System.IO;

namespace IxyCs.Pci
{
    public class PciReader : IDisposable
    {
        BinaryReader _binReader;

        public PciReader(string pciAddr, string resource)
        {
            string path = String.Format("/sys/bus/pci/devices/{0}/resource", pciAddr);
            _binReader = new BinaryReader(File.Open(path, FileMode.Open));
        }

        public uint Read32(long offset)
        {
            CheckReader();
            _binReader.BaseStream.Seek(offset, SeekOrigin.Begin);
            return _binReader.ReadUInt32();
        }

        public ushort Read16(long offset)
        {
            CheckReader();
            _binReader.BaseStream.Seek(offset, SeekOrigin.Begin);
            return _binReader.ReadUInt16();
        }

        public byte Read8(long offset)
        {
            CheckReader();
            _binReader.BaseStream.Seek(offset, SeekOrigin.Begin);
            return _binReader.ReadByte();
        }

        public void Dispose()
        {
            _binReader?.Dispose();
            _binReader = null;
        }

        private void CheckReader()
        {
            if(_binReader == null || _binReader.BaseStream == null)
                throw new InvalidOperationException("The reader is not prepared to reader.");
        }
    }
}