dotnet build
dotnet publish -c release
cd src/ixy_c
gcc -fPIC -c ixy.c -o ixy.o
gcc ixy.o -shared -o ixy_c.so
mv ixy_c.so ../../bin/Debug/netcoreapp2.0/
cp ../../bin/Debug/netcoreapp2.0/ixy_c.so ../../bin/release/netcoreapp2.0/
echo "Build complete. Run dotnet bin/release/netcoreapp2.0/IxyCs.dll to start the driver"