lib_LTLIBRARIES = libporttime.la

libporttime_la_SOURCES = \
	porttime.c  \
	porttime.h

if PLATFORM_WIN32
	libporttimr_la_SOURCES += ptwinmm.c
else
if PLATFORM_OSX
	libporttimr_la_SOURCES += ptmacosx_cf.c  ptmacosx_mach.c
else
	libporttimr_la_SOURCES += ptlinux.c
endif
endif

