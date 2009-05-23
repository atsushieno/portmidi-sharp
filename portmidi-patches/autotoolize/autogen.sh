
if [ -z "$LIBTOOL" ]; then
  LIBTOOL=`which glibtool 2>/dev/null`
  if [ ! -x "$LIBTOOL" ]; then
    LIBTOOL=`which libtool`
  fi
fi

touch AUTHORS README NEWS ChangeLog
${LIBTOOL}ize --force --copy
aclocal
autoheader
automake -a -c --add-missing
autoconf
./configure --enable-maintainer-mode --enable-compile-warnings
