using System;

namespace IxyCs.Memory
{
    public struct DmaMemory
    {
        public readonly IntPtr VirtualAddress;
        public readonly long PhysicalAddress;

        public DmaMemory(IntPtr virt, long phys)
        {
            VirtualAddress = virt;
            PhysicalAddress = phys;
        }
    }
}