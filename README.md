# Ixy.cs

ixy.cs is a C# rewrite of the [ixy](https://github.com/emmericp/ixy) userspace network driver.
It is designed to be readable, idiomatic C# code.
It supports Intel 82599 10GbE NICs (`ixgbe` family).

## Features

* See Ixy documentation

## Build instructions

[Install the .NET Core SDK](https://www.microsoft.com/net/download/linux-package-manager/ubuntu16-04/sdk-current), clone this repository and build the driver:

```
dotnet publish -c release
```

(not running in release mode will severely affect performance)

Compile the C code

```
./build_c.sh
```

Set up hugepages

```
sudo ./setup-hugetlbfs.sh
```

## Usage

Run the packet generator demo

```
sudo dotnet bin/release/netcoreapp2.0/IxyCs.dll PCI_ADDRESS
```

Replace PCI_ADDRESS with the fully qualified PCI address of your NIC, which is typically prefixed with 0000:

## Internals

`src/Ixgbe/IxgbeDevice.cs` contains the core logic.

## License

ixy.cs is licensed under the MIT license.

## Disclaimer

ixy.cs is not production-ready.
Do not use it in critical environments.
DMA may corrupt memory.

## Other languages

Check out the [other ixy implementations](https://github.com/ixy-languages).