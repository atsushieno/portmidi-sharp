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
	[DataContract]
	public class MidiModuleDatabase
	{
		public MidiModuleDatabase ()
		{
			Modules = new List<MidiModuleDefinition> ();
		}

		[DataMember]
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
