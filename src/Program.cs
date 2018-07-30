using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using IxyCs.Ixgbe;
using IxyCs.Memory;
using IxyCs.Demo;

namespace IxyCs
{
    public struct Point
    {
        public Int32 x,y;
    }
    class Program
    {

        static void Main(string[] args)
        {
            if(args[0] == "fwd")
                new Forwarder(args[1], args[2]);
            else if(args[0] == "pktgen")
                new PacketGenerator(args[1]);
            else
                Console.WriteLine("Usage:\nIxyCs.exe fwd pci_1 pci_2 Or:\nIxyCs.exe pktgen pci_1");

        }

    }
}
