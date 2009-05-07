all: mmk.exe

mmk.exe : mmk.cs
	gmcs mmk.cs ../../PortMidiSharp.cs -pkg:dotnet -resource:tonelist.txt,tonelist.txt

clean:
	rm -f mmk.exe mmk.exe.mdb
