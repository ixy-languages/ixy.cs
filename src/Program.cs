using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

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
            IntPtr ptr = Marshal.AllocHGlobal(100);
            Console.WriteLine("Virtual address: {0} - Physical address: {1}", ptr.ToInt64(),
                              Memory.MemoryHelper.VirtToPhys(ptr));
            Marshal.FreeHGlobal(ptr);
        }

    }
}
