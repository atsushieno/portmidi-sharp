using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Mono;

namespace Commons.MidiCompiler
{
	// SMF

	public class SmfMusicData
	{
		List<SmfTrack> tracks = new List<SmfTrack> ();

		public SmfMusicData ()
		{
			Format = 1;
		}

		public short DeltaTimeSpec { get; set; }

		public byte Format { get; set; }

		public void AddTrack (SmfTrack track)
		{
			this.tracks.Add (track);
		}

		public IList<SmfTrack> Tracks {
			get { return tracks; }
		}
	}

	public class SmfTrack
	{
		List<SmfEvent> events = new List<SmfEvent> ();

		public void AddEvent (SmfEvent evt)
		{
			events.Add (evt);
		}

		public IList<SmfEvent> Events {
			get { return events; }
		}
	}

	public abstract class SmfEvent
	{
		SmfEventDefinition definition;
		int delta_time;
		byte [] args;

		protected SmfEvent (SmfEventDefinition definition, int deltaTime, params byte [] args)
		{
			this.definition = definition;
			this.delta_time = deltaTime;
			this.args = args;
		}

		public SmfEventDefinition Definition {
			get { return definition; }
		}

		public abstract byte EventCode { get; }

		public int DeltaTime {
			get { return delta_time; }
		}

		public byte [] Arguments {
			get { return (byte []) args.Clone (); }
		}

		public virtual byte [] GetRawArguments ()
		{
			return args;
#if false
			if (args == null)
				return null;

			byte [] bytes = new byte [Arguments.Length + 1];
			Array.Copy (Arguments, 0, bytes, 0, Arguments.Length);
			bytes [bytes.Length - 1] = 0xF7; // EOX
			return bytes;
#endif
		}

		internal int GetVariableIntegerLength (int value)
		{
			int len = 0;
			for (int x = value; x != 0; x >>= 7)
				len++;
			return len;
		}

		internal void FillVariableInteger (byte [] buf, int index, int value)
		{
			int len = 0;
			for (int x = value; x != 0; x >>= 7)
				len++;
			for (int i = index; i < index + len; i++) {
				int bits = (i * 7);
				int tmp = value & (0xFF << bits);
				buf [i] = (byte) ((value & tmp) >> bits);
				value -= tmp;
			}
		}
	}

	public class SmfChannelEvent : SmfEvent
	{
		public SmfChannelEvent (SmfEventDefinition definition, byte channel, int deltaTime, params byte [] args)
			: base (definition, deltaTime, args)
		{
			this.channel = channel;
		}

		byte channel;

		public byte Channel {
			get { return channel; }
		}

		public override byte EventCode {
			get { return (byte) (Definition.EventType + Channel); }
		}
	}

	public class SmfMetaEvent : SmfEvent
	{
		byte meta_type;

		public SmfMetaEvent (SmfEventDefinition definition, byte metaType, int deltaTime, params byte [] args)
			: base (definition, deltaTime, args)
		{
			this.meta_type = metaType;
		}

		public byte MetaType {
			get { return meta_type; }
		}

		public override byte EventCode {
			get { return Definition.EventType; }
		}

		public override byte [] GetRawArguments ()
		{
			byte [] bytes = new byte [Arguments.Length + 1];
			bytes [0] = MetaType;
			Array.Copy (Arguments, 0, bytes, 1, Arguments.Length);
			return bytes;
		}
	}

	public class SmfEventDefinition
	{
		public static SmfEventDefinition NON = new SmfEventDefinition (
			"NoteOff", 0x80, true, false,
			new SmfEventArgumentDefinition ("key", 1, 0, 0x7F),
			new SmfEventArgumentDefinition ("vel", 1, 0, 0x7F));
		public static SmfEventDefinition NOFF = new SmfEventDefinition (
			"NoteOn", 0x90, true, false,
			new SmfEventArgumentDefinition ("key", 1, 0, 0x7F),
			new SmfEventArgumentDefinition ("vel", 1, 0, 0x7F));
		public static SmfEventDefinition PAF = new SmfEventDefinition (
			"PolyphonicAfterTouch", 0xA0, true, false,
			new SmfEventArgumentDefinition ("key", 1, 0, 0x7F),
			new SmfEventArgumentDefinition ("pressure", 1, 0, 0x7F));
		public static SmfEventDefinition CC = new SmfEventDefinition (
			"CommonControl", 0xB0, true, false,
			new SmfEventArgumentDefinition ("control", 1, 0, 0x7F),
			new SmfEventArgumentDefinition ("data", 1, 0, 0x7F));
		public static SmfEventDefinition PRG = new SmfEventDefinition (
			"ProgramChange", 0xC0, true, false,
			new SmfEventArgumentDefinition ("program", 1, 0, 0x7F));
		public static SmfEventDefinition CAF = new SmfEventDefinition (
			"ChannelAfterTouch", 0xD0, true, false,
			new SmfEventArgumentDefinition ("pressure", 1, 0, 0x7F));
		public static SmfEventDefinition PITCH = new SmfEventDefinition (
			"PitchBend", 0xE0, true, false,
			new SmfEventArgumentDefinition ("dataSmaller", 1, 0, 0x7F),
			new SmfEventArgumentDefinition ("dataLarger", 1, 0, 0x7F));
		public static SmfEventDefinition EX1 = new SmfEventDefinition (
			"Exclusive", 0xF0, false, true);
		public static SmfEventDefinition EX2 = new SmfEventDefinition (
			"Exclusive", 0xF7, false, true);
		public static SmfEventDefinition META = new SmfEventDefinition (
			"Meta", 0xFF, false, true);

		public static SmfEventDefinition FromType (int type)
		{
			switch (type) {
			case 0xF0:
				return SmfEventDefinition.EX1;
			case 0xF7:
				return SmfEventDefinition.EX2;
			case 0xFF:
				return SmfEventDefinition.META;
			}

			switch (type & 0xF0) {
			case 0x80:
				return SmfEventDefinition.NON;
			case 0x90:
				return SmfEventDefinition.NOFF;
			case 0xA0:
				return SmfEventDefinition.PAF;
			case 0xB0:
				return SmfEventDefinition.CC;
			case 0xC0:
				return SmfEventDefinition.PRG;
			case 0xD0:
				return SmfEventDefinition.CAF;
			case 0xE0:
				return SmfEventDefinition.PITCH;
			default:
				throw new FormatException (String.Format ("Unexpected MIDI operation {0:X}", type));
			}
		}

		public SmfEventDefinition (string name, byte eventType, bool channelDependent, bool useVariableArguments, params SmfEventArgumentDefinition [] args)
		{
			this.name = name;
			event_type = eventType;
			channel_dependent = channelDependent;
			this.varargs = useVariableArguments;
			this.args = new Collection<SmfEventArgumentDefinition> (args);
		}

		string name;
		byte event_type;
		bool channel_dependent;
		bool varargs;
		Collection<SmfEventArgumentDefinition> args;

		public string Name {
			get { return name; }
		}

		public byte EventType {
			get { return event_type; }
		}

		public byte MetaType { get; set; } // only for META event.

		public bool ChannelDependent {
			get { return channel_dependent; }
		}

		public bool UseVariableArguments {
			get { return varargs; }
		}

		public IList<SmfEventArgumentDefinition> Arguments {
			get { return args; }
		}
	}

	public class SmfEventArgumentDefinition
	{
		public SmfEventArgumentDefinition (string name, int size, int min, int max)
		{
			this.name = name;
			this.size = size;
			this.min = min;
			this.max = max;
		}

		string name;
		int size, min, max;

		public string Name {
			get { return name; }
		}

		public int Size {
			get { return size; }
		}

		public int MinValue {
			get { return min; }
		}

		public int MaxValue {
			get { return max; }
		}
	}

	public class SmfGenerator
	{
		BinaryWriter w;

		public SmfGenerator (Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");
			this.w = new BinaryWriter (stream);
		}

		void WriteShort (short v)
		{
			w.Write ((byte) (v / 0x100));
			w.Write ((byte) (v % 0x100));
		}

		void WriteInt (int v)
		{
			w.Write ((byte) (v / 0x1000000));
			w.Write ((byte) (v / 0x10000 & 0xFF));
			w.Write ((byte) (v / 0x100 & 0xFF));
			w.Write ((byte) (v % 0x100));
		}

		public void WriteHeader (short format, short tracks, short deltaTimeSpec)
		{
			w.Write ("MThd");
			WriteShort (0);
			WriteShort (6);
			WriteShort (format);
			WriteShort (tracks);
			WriteShort (deltaTimeSpec);
		}

		public void WriteTrack (SmfTrack track)
		{
			w.Write ("MTrk");
			WriteInt (GetTrackDataSize (track));
			foreach (SmfEvent e in track.Events) {
				Write7BitVariableInteger (e.DeltaTime);
				w.Write (e.Definition.EventType);
				if (e.Definition.UseVariableArguments) {
					foreach (int i in e.Arguments)
						w.Write ((byte) i);
				} else {
					for (int i = 0; i < e.Arguments.Length; i++) {
						int size = e.Definition.Arguments [i].Size;
						if (size == 1)
							w.Write ((byte) e.Arguments [i]);
						else if (size == 2)
							Write7BitVariableInteger (e.Arguments [i]);
						else
							throw new NotImplementedException ();
					}
				}
			}
		}

		int GetTrackDataSize (SmfTrack track)
		{
			int size = 0;
			foreach (SmfEvent e in track.Events) {
				for (int x = e.DeltaTime; x != 0; x >>= 7)
					size++;
				size++;
				if (e.Definition.UseVariableArguments)
					size += e.Arguments.Length;
				else
					foreach (SmfEventArgumentDefinition a in e.Definition.Arguments)
						size += a.Size;
			}
			return size;
		}

		void Write7BitVariableInteger (int value)
		{
			int len = 0;
			for (int x = value; x != 0; x >>= 7)
				len++;
			for (int i = 0; i < len; i++) {
				int bits = (i * 7);
				int tmp = value & (0xFF << bits);
				w.Write ((byte) ((value & tmp) >> bits));
				value -= tmp;
			}
		}
	}

	public class SmfParser
	{
		public SmfParser (Stream stream)
		{
			this.stream = stream;
		}

		Stream stream;
		SmfMusicData data = new SmfMusicData ();

		public SmfMusicData MusicData { get { return data; } }

		public void Parse ()
		{
			if (
			    ReadByte ()  != 'M'
			    || ReadByte ()  != 'T'
			    || ReadByte ()  != 'h'
			    || ReadByte ()  != 'd')
				throw ParseError ("MThd is expected");
			if (ReadInt32 () != 6)
				throw ParseError ("Unexpeted data size (should be 6)");
			data.Format = (byte) ReadInt16 ();
			int tracks = ReadInt16 ();
			data.DeltaTimeSpec = ReadInt16 ();
			try {
				for (int i = 0; i < tracks; i++)
					data.Tracks.Add (ReadTrack ());
			} catch (FormatException ex) {
				throw ParseError ("Unexpected data error", ex);
			}
		}

		SmfTrack ReadTrack ()
		{
			var tr = new SmfTrack ();
			if (
			    ReadByte ()  != 'M'
			    || ReadByte ()  != 'T'
			    || ReadByte ()  != 'r'
			    || ReadByte ()  != 'k')
				throw ParseError ("MTrk is expected");
			int trackSize = ReadInt32 ();
			current_track_size = 0;
			int total = 0;
			while (current_track_size < trackSize) {
				int delta = ReadVariableLength ();
				tr.Events.Add (ReadEvent (delta));
				total += delta;
			}
			return tr;
		}

		int current_track_size;
		byte running_status;

		SmfEvent ReadEvent (int deltaTime)
		{
			byte b = PeekByte ();
			running_status = b < 0x80 ? running_status : ReadByte ();
			var def = SmfEventDefinition.FromType (running_status);
			byte metaType = running_status == 0xFF ? ReadByte () : (byte) 0;
			int len;
			if (def.UseVariableArguments)
				len = ReadVariableLength ();
			else
				len = def.Arguments.Count;
			byte [] args = new byte [len];
			if (len > 0)
				ReadBytes (args);
			if (running_status == 0xFF)
				return new SmfMetaEvent (def, metaType, deltaTime, args);
			else
				return new SmfChannelEvent (def, (byte) (running_status - def.EventType) , deltaTime, args);
		}

		void ReadBytes (byte [] args)
		{
			current_track_size += args.Length;
			int start = 0;
			if (peek_byte >= 0) {
				args [0] = (byte) peek_byte;
				peek_byte = -1;
				start = 1;
			}
			int len = stream.Read (args, start, args.Length - start);
			try {
			if (len < args.Length - start)
				throw ParseError (String.Format ("The stream is insufficient to read {0} bytes specified in the SMF event. Only {1} bytes read.", args.Length, len));
			} finally {
				stream_position += len;
			}
		}

		int ReadVariableLength ()
		{
			int val = 0;
			for (int i = 0; i < 4; i++) {
				byte b = ReadByte ();
				val = (val << 7) + b;
				if (b < 0x80)
					return val;
				val -= 0x80;
			}
			throw ParseError ("Delta time specification exceeds the 4-byte limitation.");
		}

		int peek_byte = -1;
		int stream_position;

		byte PeekByte ()
		{
			if (peek_byte < 0)
				peek_byte = stream.ReadByte ();
			if (peek_byte < 0)
				throw ParseError ("Insufficient stream. Failed to read a byte.");
			return (byte) peek_byte;
		}

		byte ReadByte ()
		{
			try {

			current_track_size++;
			if (peek_byte >= 0) {
				byte b = (byte) peek_byte;
				peek_byte = -1;
				return b;
			}
			int ret = stream.ReadByte ();
			if (ret < 0)
				throw ParseError ("Insufficient stream. Failed to read a byte.");
			return (byte) ret;

			} finally {
				stream_position++;
			}
		}

		short ReadInt16 ()
		{
			return (short) ((ReadByte () << 8) + ReadByte ());
		}

		int ReadInt32 ()
		{
			return (((ReadByte () << 8) + ReadByte () << 8) + ReadByte () << 8) + ReadByte ();
		}

		struct SmfDeltaTime
		{
			int size, delta;
			public SmfDeltaTime (int size, int deltaTime)
			{
				this.size = size;
				this.delta = deltaTime;
			}

			public int Size { get { return size; } }
			public int DeltaTime  { get { return delta; } }
		}

		Exception ParseError (string msg)
		{
			return ParseError (msg, null);
		}

		Exception ParseError (string msg, Exception innerException)
		{
			throw new SmfParserException (String.Format (msg + "(at {0})", stream_position), innerException);
		}
	}

	public class SmfParserException : Exception
	{
		public SmfParserException () : this ("SMF parser error") {}
		public SmfParserException (string message) : base (message) {}
		public SmfParserException (string message, Exception innerException) : base (message, innerException) {}
	}
}
