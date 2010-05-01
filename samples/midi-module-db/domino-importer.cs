// It is a utility to import module definition files for Domino MIDI sequencer: http://parupu.chu.jp/domino/
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Commons.Music.Midi.ModuleDatabase
{
	static class XLinqExtensions
	{
		public static string Attr (this XElement el, string localName)
		{
			return (string) el.Attribute (XName.Get (localName));
		}

		public static int AttrAsInt (this XElement el, string localName)
		{
			return (int) el.Attribute (XName.Get (localName));
		}
	}

	public class DominoModuleXmlImporter
	{
		public static void Main (string [] args)
		{
			var imp = new DominoModuleXmlImporter ();
			var ds = new DataContractJsonSerializer (typeof (MidiModuleDefinition));
			foreach (string arg in args) {
				var db = imp.Import (arg);
				var ms = new MemoryStream ();
				ds.WriteObject (ms, db.Modules [0]);
				ms.Position = 0;
				var s = new StreamReader (ms).ReadToEnd ();
				s = s.Replace ("},", "},\n");
				using (var sw = File.CreateText (Path.ChangeExtension (arg, "midimod")))
					sw.Write (s);
			}
		}

		public MidiModuleDatabase Import (string file)
		{
			var doc = XDocument.Load (file);
			var db = new MidiModuleDatabase ();
			foreach (var mod in doc.Elements ("ModuleData")) {
				var mdd = new MidiModuleDefinition () { Name = mod.Attr ("Name") };
				db.Modules.Add (mdd);
				foreach (var ilist in mod.Elements ("InstrumentList"))
					foreach (var map in ilist.Elements ("Map")) {
						var mad = new MidiInstrumentMap () { Name = map.Attr ("Name") };
						mdd.Instrument.Maps.Add (mad);
						foreach (var pc in map.Elements ("PC")) {
							var pd = new MidiProgramDefinition () { Name = pc.Attr ("Name"), Index = pc.AttrAsInt ("PC") };
							mad.Programs.Add (pd);
							foreach (var bank in pc.Elements ("Bank")) {
								if (bank.Attr ("MSB") == null)
									// Domino XML definition contains extra bank element that mimics mapping. We have to skip it.
									continue;
								pd.Banks.Add (new MidiBankDefinition () {  Name = bank.Attr ("Name"), Msb = bank.AttrAsInt ("MSB"), Lsb = bank.AttrAsInt ("LSB") });
							}
						}
					}
			}
			return db;
		}
	}
}
