gcc -mno-cygwin -mwindows -c -o pmwin.o -I ../pm_common -I ../porttime pmwin.c
gcc -mno-cygwin -mwindows -c -o pmwinmm.o -I ../pm_common -I ../porttime pmwinmm.c
gcc -mno-cygwin -mwindows -c -o portmidi.o -I ../pm_common -I ../porttime ../pm_common/portmidi.c
gcc -mno-cygwin -mwindows -c -o pmutil.o -I ../pm_common -I ../porttime ../pm_common/pmutil.c
dllwrap --target i386-mingw32 --export-all --output-def portmidi.def --implib libportmidi.a --driver-name gcc -mno-cygwin -mwindows -I ../pm_common -I ../porttime pmwin.o pmwinmm.o portmidi.o pmutil.o -lwinmm -L../porttime -lporttime -o portmidi.dll
