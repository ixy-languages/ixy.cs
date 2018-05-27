using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
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
            //  IntPtr ptr = Marshal.AllocHGlobal(100);
            //  Console.WriteLine("Virtual address: {0} - Physical address: {1}", ptr.ToInt64(),
            //                    Memory.MemoryHelper.VirtToPhys(ptr));
            //  Marshal.FreeHGlobal(ptr);
            var dmaMem = MemoryHelper.AllocateDma(4096*16, true);
            Console.WriteLine("Phys: {0} // Virt: {1}", dmaMem.PhysicalAddress, dmaMem.VirtualAddress);
        }

    }
}
