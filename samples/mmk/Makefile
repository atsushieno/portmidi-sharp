all: mmk.exe

mmk.exe : mmk.cs
	gmcs mmk.cs ../../PortMidiSharp.cs -pkg:dotnet

clean:
	rm -f mmk.exe mmk.exe.mdb
