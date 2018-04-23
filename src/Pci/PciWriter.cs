using System;
using System.IO;

namespace IxyCs.Pci
{
    public class PciWriter : IDisposable
    {
        BinaryWriter _binWriter;

        public PciWriter(string pciAddr, string resource)
        {
            string path = String.Format("/sys/bus/pci/devices/{0}/resource");
            _binWriter = new BinaryWriter(File.Open(path, FileMode.Open));
        }

        public void Dispose()
        {
            _binWriter.Dispose();
            _binWriter = null;
        }

        //Bit size in each overload name is redundant but useful to force the user to think of their data type

        public void Write8(byte value, long offset)
        {
            CheckWriter();
            _binWriter.BaseStream.Seek(offset, SeekOrigin.Begin);
            _binWriter.Write(value);
        }

        public void Write16(uint value, long offset)
        {
            CheckWriter();
            _binWriter.BaseStream.Seek(offset, SeekOrigin.Begin);
            _binWriter.Write(value);
        }

        public void Write32(int value, long offset)
        {
            CheckWriter();
            _binWriter.BaseStream.Seek(offset, SeekOrigin.Begin);
            _binWriter.Write(value);
        }

        private void CheckWriter()
        {
            if(_binWriter == null || _binWriter.BaseStream == null)
                throw new InvalidOperationException("The writer is not prepared to write.");
        }
    }
}