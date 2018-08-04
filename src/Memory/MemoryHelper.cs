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

        //NOT WORKING - USE AllocateDmaC INSTEAD
        /*
        public unsafe static DmaMemory AllocateDma(int size, bool requireContiguous)
        {
            //Round up to multiples of 2MB
            if(size % HugePageSize != 0)
                size = ((size >> HugePageBits) + 1) << HugePageBits;

            if(requireContiguous && size > HugePageSize)
            {
                Log.Error("Could not map physically contiguous memory of size {0}", size);
                Environment.Exit(1);
            }
            HugePageNumber++;
            int pid = Process.GetCurrentProcess().Id;
            string path = String.Format("/mnt/huge/ixycs-{0}-{1}", pid, HugePageNumber);

            //Create hugepage file
            while(File.Exists(path))
            {
                HugePageNumber++;
                path = String.Format("/mnt/huge/ixycs-{0}-{1}", pid, HugePageNumber);
            }
            try
            {
                using(FileStream stream = File.Create(path))
                {
                    stream.SetLength(size);
                }
            }
            catch(IOException e)
            {
                Log.Error("Could not create hugetlbfs file - {0}", e.Message);
                Environment.Exit(1);
            }

            //Get pointer to mapped file
            //The c version uses PROT_READ | PROT_WRITE and MAP_SHARED | MMAG_HUGETLB flags
            var memMap = MemoryMappedFile.CreateFromFile(path, FileMode.Open);
            var accessor = memMap.CreateViewAccessor(0, 0);

            byte *virtAddr = (byte*)0;
            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref virtAddr);

            //TODO : The c version uses mlock here to stop the memory at virtAddr from being swapped
            //Also may want to delete hugepage file


            //BUG: VirtToPhys doesn't seem to work for our mmap pointer
            //(although pointer is valid and VirtToPhys works on pointers allocated with AlocHGlobal)
            //might need to call this entire function in C, unfortunately
            return new DmaMemory((long)virtAddr, VirtToPhys(virtAddr));
        }
        */

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