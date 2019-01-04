using System;
using System.Diagnostics;
using IxyCs.Ixgbe;
using IxyCs.Memory;

namespace IxyCs.Demo
{
    //This is a demo which shows how to use Ixy.Cs in a forwarding application
    public class Forwarder
    {
        public const int BatchSize = 32;
        //Since we are just using one queue, we can save the mempool here
        private Mempool _mempool;

        public unsafe Forwarder(string pci1, string pci2)
        {
            if(String.IsNullOrEmpty(pci1) || String.IsNullOrEmpty(pci2))
            {
                Log.Error("Please provide two pci addresses");
                Environment.Exit(1);
            }

            var dev1 = new IxgbeDevice(pci1, 1, 1);
            var dev2 = new IxgbeDevice(pci2, 1, 1);

            // TODO: switch to C# 7.3 and replace with Span<PacketBuffer> buffers = stackalloc PacketBuffer[BatchSize];
            var buffersArray = stackalloc PacketBuffer[BatchSize];
            var buffers = new Span<PacketBuffer>(buffersArray, BatchSize);

            ulong counter = 0;
            var stopWatch = new Stopwatch();
            var stats1 = new DeviceStats(dev1);
            var stats1Old = new DeviceStats(dev1);
            var stats2 = new DeviceStats(dev2);
            var stats2Old = new DeviceStats(dev2);
            stopWatch.Start();
            while(true)
            {
                Forward(dev1, 0, dev2, 0, buffers);
                Forward(dev2, 0, dev1, 0, buffers);
                stats1.Reset();
                stats1Old.Reset();
                stats2.Reset();
                stats2Old.Reset();

                //Periodically measure time
                if(((counter++ % 500000) == 0) && stopWatch.ElapsedMilliseconds > 1000)
                {
                    stopWatch.Stop();
                    var nanos = stopWatch.ElapsedTicks;
                    dev1.ReadStats(ref stats1);
                    stats1.PrintStatsDiff(ref stats1Old, (ulong)nanos);
                    stats1Old = stats1;
                    if(dev1 != dev2)
                    {
                        dev2.ReadStats(ref stats2);
                        stats2.PrintStatsDiff(ref stats2Old, (ulong)nanos);
                        stats2Old = stats2;
                    }
                    counter = 0;
                    stopWatch.Restart();
                }
            }
        }

        private void Forward(IxgbeDevice rxDev, int rxQueue, IxgbeDevice txDev, int txQueue, Span<PacketBuffer> buffers)
        {
            var rxBuffCount = rxDev.RxBatch(rxQueue, buffers);
            if(rxBuffCount > 0)
            {
                var rxBuffers = buffers.Slice(0, rxBuffCount);
                //Touch all buffers to simulate a realistic scenario
                foreach (var buffer in rxBuffers)
                    buffer.Touch();

                var txBuffCount = txDev.TxBatch(txQueue, rxBuffers);
                _mempool = (_mempool == null) ? Mempool.FindPool(rxBuffers[0].MempoolId) : _mempool;

                //Drop unsent packets
                foreach (var buffer in rxBuffers.Slice(txBuffCount))
                    _mempool.FreeBuffer(buffer);
            }
        }
    }
}