using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Commons.Music.Midi.ModuleDatabase
{
	public abstract class MidiModuleDatabase
	{
		public static readonly MidiModuleDatabase Default = new DefaultMidiModuleDatabase ();
		
		public abstract MidiModuleDefinition Resolve (string moduleName);
	}

	class DefaultMidiModuleDatabase : MidiModuleDatabase
	{
		public DefaultMidiModuleDatabase ()
		{
			Modules = new List<MidiModuleDefinition> ();
			foreach (string file in Directory.GetFiles (new Uri (GetType ().Assembly.CodeBase).LocalPath, "*.midimod"))
				Modules.Add (MidiModuleDefinition.Load (file));
		}

		public override MidiModuleDefinition Resolve (string moduleName)
		{
			string name = ResolvePossibleAlias (moduleName);
			return Modules.FirstOrDefault (m => m.Name == name);
		}

		public string ResolvePossibleAlias (string name)
		{
			switch (name) {
			case "Microsoft GS Wavetable Synth":
				return "Microsoft GS Wavetable SW Synth";
			}
			return name;
		}

		public IList<MidiModuleDefinition> Modules { get; private set; }
	}

	[DataContract]
	public class MidiModuleDefinition
	{
		public MidiModuleDefinition ()
		{
			Instrument = new MidiInstrumentDefinition ();
		}

		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public MidiInstrumentDefinition Instrument { get; set; }

		// serialization

		public void Save (string file)
		{
			using (var fs = File.OpenWrite (file))
				Save (fs);
		}

		public void Save (Stream stream)
		{
			var ds = new DataContractJsonSerializer (typeof (MidiModuleDefinition));
			ds.WriteObject (stream, this);
		}

		public static MidiModuleDefinition Load (string file)
		{
			using (var fs = File.OpenRead (file))
				return Load (fs);
		}

		public static MidiModuleDefinition Load (Stream stream)
		{
			var ds = new DataContractJsonSerializer (typeof (MidiModuleDefinition));
			return (MidiModuleDefinition) ds.ReadObject (stream);
		}
	}

	[DataContract]
	public class MidiInstrumentDefinition
	{
		public MidiInstrumentDefinition ()
		{
			Maps = new List<MidiInstrumentMap> ();
		}

		[DataMember]
		public IList<MidiInstrumentMap> Maps { get; private set; }
	}

	[DataContract]
	public class MidiInstrumentMap
	{
		public MidiInstrumentMap ()
		{
			Programs = new List<MidiProgramDefinition> ();
		}
		
		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public IList<MidiProgramDefinition> Programs { get; private set; }
	}

	[DataContract]
	public class MidiProgramDefinition
	{
		public MidiProgramDefinition ()
		{
			Banks = new List<MidiBankDefinition> ();
		}

		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public int Index { get; set; }
		[DataMember]
		public IList<MidiBankDefinition> Banks { get; private set; }
	}

	[DataContract]
	public class MidiBankDefinition
	{
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public int Msb { get; set; }
		[DataMember]
		public int Lsb { get; set; }
	}
}
