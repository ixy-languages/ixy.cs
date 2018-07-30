using System;

namespace IxyCs.Memory
{
    public class DmaMemory
    {
        public readonly long VirtualAddress;
        public readonly long PhysicalAddress;

        public DmaMemory(long virt, long phys)
        {
            VirtualAddress = virt;
            PhysicalAddress = phys;
        }
    }
}