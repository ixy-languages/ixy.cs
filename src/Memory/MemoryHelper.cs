using System;
using System.IO;
using System.Runtime.InteropServices;

namespace IxyCs.Memory
{
    public static class MemoryHelper
    {
        public static long VirtToPhys(IntPtr virt)
        {
            long pageSize = Environment.SystemPageSize;
            long physical = 0;
            //TODO : Types are most likely wrong here
            //Read page from /proc/self/pagemap
            try
            {
                using(BinaryReader reader = new BinaryReader(File.Open("/proc/self/pagemap", FileMode.Open)))
                {
                    reader.BaseStream.Seek(virt.ToInt64() / pageSize * sizeof(long),
                    SeekOrigin.Begin);
                    physical = reader.ReadInt64();
                }
            }
            catch(Exception ex)
            {
                Log.Error("FATAL: Could not translate virtual address {0} to physical address. - {1}", virt, ex.Message);
                Environment.Exit(1);
            }
            return (physical & 0x7fffffffffffffL) * pageSize + virt.ToInt64() % pageSize;
        }
    }
}