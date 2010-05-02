RUNTIME = mono

domino-importer.exe : domino-importer.cs MidiModuleDatabase.cs
	gmcs domino-importer.cs MidiModuleDatabase.cs -pkg:wcf -r:System.Xml.Linq

import: domino-importer.exe
	for fff in *.xml ; do $(RUNTIME) ./domino-importer.exe $$fff ; done;

clean:
	rm -f domino-importer.exe

