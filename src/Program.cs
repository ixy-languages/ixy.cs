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
        [DllImport("ixy_c.so")]
        public static extern IntPtr dma_memory(int size, bool requireContiguous);
        [DllImport("ixy_c.so")]
        public static extern IntPtr virt_to_phys(IntPtr ptr);

        static void Main(string[] args)
        {
            //  IntPtr ptr = Marshal.AllocHGlobal(100);
            //  Console.WriteLine("Virtual address: {0} - Physical address: {1}", ptr.ToInt64(),
            //                    Memory.MemoryHelper.VirtToPhys(ptr));
            //  Marshal.FreeHGlobal(ptr);
            //var dmaMem = MemoryHelper.AllocateDma(4096*16, true);
            //Console.WriteLine("Phys: {0} // Virt: {1}", dmaMem.PhysicalAddress, dmaMem.VirtualAddress);
            var ptr = dma_memory(4096*16, true);
            Console.WriteLine("C dma_mem: Virtual address: {0:X}", ptr);
            Console.WriteLine("C# VirtToPhys: Physical address: {0:X}", Memory.MemoryHelper.VirtToPhys(ptr));
            //var mem = MemoryHelper.AllocateDma(4096*16, false);
            //Console.WriteLine("C# AllocateDma: Virt: {0:X} // Phys: {1:X}", mem.VirtualAddress, mem.PhysicalAddress);
            //Console.WriteLine("C: Physical address C: {0:X}", virt_to_phys(ptr));
            //var csptr = Marshal.AllocHGlobal(8);
            //Console.WriteLine("C#: Virtual address: {0:X}", csptr);
            //Console.WriteLine("C#: Physical address: {0:X}", Memory.MemoryHelper.VirtToPhys(csptr));
        }

    }
}
