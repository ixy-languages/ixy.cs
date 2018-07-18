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
            //new Forwarder(args[0], args[1]);
            new PacketGenerator(args[0]);
        }

    }
}
