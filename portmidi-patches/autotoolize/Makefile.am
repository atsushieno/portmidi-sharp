AUTOMAKE_OPTIONS = foreign
ACLOCAL_AMFLAGS = -I .

if PLATFORM_WIN32
SUBDIRS = porttime pm_win pm_test
else
if PLATFORM_OSX
SUBDIRS = porttime pm_mac pm_test
else
SUBDIRS = porttime pm_linux pm_test
endif
endif
