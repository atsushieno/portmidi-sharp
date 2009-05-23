touch AUTHORS README NEWS ChangeLog
glibtoolize --force --copy
aclocal
autoheader
automake -a -c --add-missing
autoconf
./configure
