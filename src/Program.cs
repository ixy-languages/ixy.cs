using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace IxyCs
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            /*using(var mmf = MemoryMappedFile.CreateFromFile("/home/max/Desktop/tmp/shared",
                                                            FileMode.Open))
            {
                using(var accessor = mmf.CreateViewAccessor(0, 0))
                {
                    var read = accessor.ReadByte(0);
                    accessor.Write(1, read + 1);
                }
            }*/
        }

    }
}
