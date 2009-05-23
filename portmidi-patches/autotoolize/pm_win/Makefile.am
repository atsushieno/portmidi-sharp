lib_LTLIBRARIES = libporttime.la libportmidi.la

COMMON_CFLAGS = -mno-cygwin -g
COMMON_LDFLAGS = -mno-cygwin -mms-bitfields -mwindows -shared -no-undefined -lwinmm

libporttime_la_SOURCES = \
	../porttime/porttime.c \
	../porttime/ptwinmm.c

libporttime_la_CFLAGS = $(COMMON_CFLAGS) -I ../porttime
libporttime_la_LDFLAGS = $(COMMON_LDFLAGS)

libportmidi_la_SOURCES = \
	../pm_common/pmutil.c  \
	../pm_common/portmidi.c  \
	pmwin.c  \
	pmwinmm.c

libportmidi_la_CFLAGS = $(COMMON_CFLAGS) -I ../pm_common -I ../porttime
libportmidi_la_LDFLAGS = $(COMMON_LDFLAGS) -L.libs -lporttime

