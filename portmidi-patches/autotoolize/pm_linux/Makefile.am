AM_CFLAGS = -DPM_CHECK_ERRORS -DPMALSA -I ../porttime

#noinst_PROGRAMS = ../pm_test/test ../pm_test/sysex ../pm_test/midithread \
#	../pm_test/latency ../pm_test/midithru ../pm_test/qtest ../pm_test/mm

lib_LTLIBRARIES = libporttime.la libportmidi.la

libporttime_la_CFLAGS = -DPM_CHECK_ERRORS -DPMALSA -I ../porttime

libporttime_la_SOURCES = \
	../porttime/porttime.c \
	../porttime/ptlinux.c

libportmidi_la_CFLAGS = -DPM_CHECK_ERRORS -DPMALSA -I ../porttime -I ../pm_common
libportmidi_la_LDFLAGS = -lporttime -lasound

libportmidi_la_SOURCES = \
	$(libporttime_la_SOURCES) \
	../pm_common/portmidi.c \
	../pm_common/pmutil.c \
	finddefault.c \
	pmlinux.c \
	pmlinuxalsa.c
