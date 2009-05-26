all: mmk.exe mmchromakey.exe

mmk.exe : mmk.cs
	gmcs mmk.cs ../../PortMidiSharp.cs -pkg:dotnet -resource:tonelist.txt,tonelist.txt

mmchromakey.exe : mmk.cs
	gmcs mmk.cs -out:mmchromakey.exe ../../PortMidiSharp.cs -pkg:dotnet -resource:tonelist.txt,tonelist.txt -d:CHROMA_TONE

clean:
	rm -f mmk.exe mmk.exe.mdb
	rm -f mmchromakey.exe mmchromakey.exe.mdb
