dotnet build
dotnet publish -c release
cd src/ixy_c
gcc -fPIC -c ixy.c -o ixy.o
gcc ixy.o -shared -o ixy_c.so
mv ixy_c.so ../../bin/Debug/net6.0/
cp ../../bin/Debug/net6.0/ixy_c.so ../../bin/release/net6.0/
echo "Build complete. Run dotnet bin/release/net6.0/IxyCs.dll to start the driver"
