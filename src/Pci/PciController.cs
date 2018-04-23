using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace IxyCs.Pci
{
    public class PciController
    {
        public static void RemoveDriver(string pciAddr)
        {
            string path = string.Format("/sys/bus/pci/devices/{0}/driver/unbind", pciAddr);
            if(!File.Exists(path))
            {
                Log.Notice("There was no driver loaded for {0}", pciAddr);
                return;
            }
            try
            {
                using(var writer = new StreamWriter(path))
                {
                    writer.Write(pciAddr);
                }
            }
            catch(Exception e)
            {
                if(e is IOException || e is UnauthorizedAccessException)
                    Log.Warning("Could not unload driver for {0} - {1}", pciAddr, e.Message);
                else
                    throw e;
            }
        }

        public static void EnableDma(string pciAddr)
        {
            try
            {
                using(var stream = new FileStream(
                    String.Format("/sys/bus/pci/devices/{0}/config", pciAddr), FileMode.Open))
                {
                    //Read 2 bytes with 4 byte offset
                    stream.Seek(4, SeekOrigin.Begin);
                    byte[] bytes = new byte[2];
                    if(stream.Read(bytes, 0, 2) < 2)
                    {
                        Log.Warning("Could not enable DMA");
                        return;
                    }
                    ushort dma = BitConverter.ToUInt16(bytes, 0);
                    //Bit 2 is "bus master enable" (PCIe 3.0 specs 7.5.1.1)
                    dma |= 1 << 2;

                    //Write bytes back to config
                    stream.Seek(4, SeekOrigin.Begin);
                    stream.Write(BitConverter.GetBytes(dma), 0, 2);
                }
            }
            catch(Exception e)
            {
                Log.Warning("Could not enable DMA - {0}", e.Message);
                return;
            }
        }

        public static MemoryMappedFile MapResource(string pciAddr)
        {
            return MemoryMappedFile.CreateFromFile(
                string.Format("/sys/bus/pci/devices/{0}/resource0", pciAddr), FileMode.Open);
        }
    }
}