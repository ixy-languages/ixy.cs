cd src/ixy_c
gcc -c ixy.c -o ixy.o
gcc ixy.o -shared -o ixy_c.so
mv ixy_c.so ../../bin/Debug/netcoreapp2.0/