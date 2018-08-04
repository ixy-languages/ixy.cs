using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace IxyCs.Memory
{
    public static class MemoryHelper
    {
        public const int HugePageBits = 21;
        public const int HugePageSize = 1 << HugePageBits;

        //private static int HugePageNumber = 0;

        [DllImport("ixy_c.so")]
        public static extern ulong dma_memory(uint size, bool requireContiguous);

        public static ulong VirtToPhys(ulong virt)
        {
            ulong pageSize = (ulong)Environment.SystemPageSize;
            ulong physical = 0;
            //Read page from /proc/self/pagemap
            try
            {
                using(BinaryReader reader = new BinaryReader(File.Open("/proc/self/pagemap", FileMode.Open)))
                {
                    ulong pos = virt / pageSize * sizeof(ulong);
                    reader.BaseStream.Seek((long)pos,
                    SeekOrigin.Begin);
                    physical = reader.ReadUInt64();
                }
            }
            catch(Exception ex)
            {
                Log.Error("FATAL: Could not translate virtual address {0:X} to physical address. - {1}", virt, ex.Message);
                Environment.Exit(1);
            }
            return (physical & 0x7fffffffffffffL) * pageSize + virt % pageSize;
        }

        public static DmaMemory AllocateDmaC(uint size, bool requireContiguous)
        {
            var virt = dma_memory(size, requireContiguous);
            return new DmaMemory(virt, VirtToPhys(virt));
        }

        public static Mempool AllocateMempool(uint numEntries, uint entrySize = 2048)
        {
            if(HugePageSize % entrySize != 0)
            {
                Log.Error("FATAL: Entry size must be a divisor of the huge page size {0}", HugePageSize);
                Environment.Exit(1);
            }

            var dma = AllocateDmaC(numEntries * entrySize, false);
            var mempool = new Mempool(dma.VirtualAddress, entrySize, numEntries);
            mempool.PreallocateBuffers();
            return mempool;
        }
    }
}