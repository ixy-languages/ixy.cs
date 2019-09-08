# Ixy.cs

ixy.cs is a C# rewrite of the [ixy](https://github.com/emmericp/ixy) userspace network driver.
It is designed to be readable, idiomatic C# code.
It supports Intel 82599 10GbE NICs (`ixgbe` family).

## Features

* See Ixy documentation

## Build instructions

[Install the .NET Core SDK](https://www.microsoft.com/net/download/linux-package-manager/ubuntu16-04/sdk-current) (if you're on Ubuntu 16.10, you can use the dotnet_install_ubuntu_16.sh script).

Clone this repository and build the driver:


    ./build.sh


This will build the driver in debug and release configuration and compile the C code.

Set up hugepages

    sudo ./setup-hugetlbfs.sh

## Usage

Run the packet generator demo


    sudo dotnet bin/release/netcoreapp2.1/IxyCs.dll pktgen PCI_ADDRESS

OR

    sudo dotnet bin/release/netcoreapp2.1/IxyCs.dll fwd PCI_1 PCI_2



Replace PCI_ADDRESS with the fully qualified PCI address of your NIC, which is typically prefixed with `0000:`

**Using dotnet run or the debug configuration severely decreases performance!**

## Internals

`src/Ixgbe/IxgbeDevice.cs` contains the core logic. The demos are in `src/Demo`. The `Ixgbe/IxgbeDefs.cs` file contains a huge amount of NIC-related constants. Most of these are not used in this driver.

## License

ixy.cs is licensed under the MIT license.

## Disclaimer

**Ixy.cs is not production-ready. Do not use it in critical environments or on any systems with data you don't want to lose!**

## Other languages

Check out the [other ixy implementations](https://github.com/ixy-languages).
