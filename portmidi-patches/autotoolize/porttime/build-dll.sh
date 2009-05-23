# gcc -mno-cygwin -shared -o porttime.dll -no-undefined -Wl,--kill-at porttime. h porttime.c ptwinmm.c -lwinmm
# gcc -shared -mno-cygwin -mwindows -Wl,-soname -Wl,libporttime.so.0 porttime.o ptwinmm.o -o libportmidi.so.0.0.0 -lwinmm
gcc -mno-cygwin -mwindows -c porttime.c -o porttime.o
gcc -mno-cygwin -mwindows -c ptwinmm.c -o ptwinmm.o
dllwrap --target i386-mingw32 --export-all  --output-def porttime.def --implib libporttime.a --driver-name gcc -mno-cygwin -mwindows -o porttime.dll porttime.o ptwinmm.o -lwinmm
