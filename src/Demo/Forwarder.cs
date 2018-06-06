using System;

namespace IxyCs.src.Demo
{
    /*
        This is a demo which shows how to use Ixy.Cs in a forwarding application
     */
    public class Forwarder
    {
        public Forwarder(string pci1, string pci2)
        {
            if(String.IsNullOrEmpty(pci1) || String.IsNullOrEmpty(pci2))
            {
                Log.Error("Please provide two pci addresses");
                Environment.Exit(1);
            }
            var dev1 = new Ixgbe.IxgbeDevice(pci1, 1, 1);
            var dev2 = new Ixgbe.IxgbeDevice(pci2, 1, 1);
        }
    }
}