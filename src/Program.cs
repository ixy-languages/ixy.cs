using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using IxyCs.Ixgbe;
using IxyCs.Memory;

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
            new IxgbeDevice(args[0], 1, 1);
        }

    }
}
