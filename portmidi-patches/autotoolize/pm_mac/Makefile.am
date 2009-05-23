
lib_LTLIBRARIES = libporttime.la libportmidi.la

libporttime_la_CFLAGS = -I ../porttime

libporttime_la_SOURCES = \
	../porttime/porttime.c \
	../porttime/ptmacosx_mach.c

libportmidi_la_CFLAGS = -I ../porttime -I ../pm_common -I/Developer/Headers/FlatCarbon
libportmidi_la_LDFLAGS = -lporttime \
	-framework CoreMIDI \
	-framework CoreFoundation \
	-framework CoreAudio \
	-framework CoreServices

libportmidi_la_SOURCES = \
	../pm_common/portmidi.c ../pm_common/pmutil.c \
	pmmac.c pmmacosxcm.c readbinaryplist.c finddefault.c

MACLIB = -framework CoreMIDI -framework CoreFoundation -framework CoreAudio \
	 -framework CoreServices

current: all


