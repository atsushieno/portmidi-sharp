AUTOMAKE_OPTIONS = foreign
ACLOCAL_AMFLAGS = -I .

if PLATFORM_WIN32
SUBDIRS = pm_win pm_test
else
if PLATFORM_OSX
SUBDIRS = pm_mac pm_test
else
SUBDIRS = pm_linux pm_test
endif
endif
