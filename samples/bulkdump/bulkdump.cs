using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using PortMidiSharp;

namespace Commons.Music.Midi
{
	public class Driver
	{
		public static void Main (string [] args)
		{
			int inId = -1, outId = -1;
			string filename = null;
			TimeSpan interval = TimeSpan.Zero;
			byte [] ex = null;
			foreach (var arg in args) {
				if (arg.StartsWith ("--in:")) {
					if (!int.TryParse (arg.Substring (5), out inId))
						inId = -1;
				}
				if (arg.StartsWith ("--out:")) {
					if (!int.TryParse (arg.Substring (6), out outId))
						outId = -1;
				}
				if (arg.StartsWith ("--file:"))
					filename = arg.Substring (7);
				if (arg.StartsWith ("--interval:")) {
					int intervalint;
					if (int.TryParse (arg.Substring (11), out intervalint))
						interval = TimeSpan.FromMilliseconds (intervalint);
				}
				if (arg.StartsWith ("--sysex:")) {
					string [] l = arg.Substring (8).Split (',');
					ex = new byte [l.Length];
					byte v;
					for (int i = 0; i < ex.Length; i++) {
						if (byte.TryParse (l [i], NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out v))
							ex [i] = v;
						else {
							ex = null;
							break;
						}
					}
					if (ex == null) {
						Console.WriteLine ("invalid sysex: " + arg);
						return;
					}
				}
			}
			var a = new List<MidiDeviceInfo> (MidiDeviceManager.AllDevices);
			if (inId < 0) {
				foreach (var dev in a)
					if (dev.IsInput)
						Console.WriteLine ("ID {0}: {1}", dev.ID, dev.Name);
				Console.WriteLine ("Type number to select MIDI In Device to use (type anything else to quit)");
				if (!int.TryParse (Console.ReadLine (), out inId))
					return;
			}
			if (outId < 0) {
				foreach (var dev in a)
					if (dev.IsOutput)
						Console.WriteLine ("ID {0}: {1}", dev.ID, dev.Name);
				Console.WriteLine ("Type number to select MIDI Out Device to use (type anything else to quit)");
				if (!int.TryParse (Console.ReadLine (), out outId))
					return;
			}

			var dump = new BulkDump ();
			if (interval != TimeSpan.Zero)
				dump.Interval = interval;
			if (ex != null)
				dump.SetSysEx (ex);
			dump.Start (inId, outId);
			Console.WriteLine ("Type [CR] to stop receiving");
			Console.ReadLine ();
			dump.Stop ();

			if (String.IsNullOrEmpty (filename)) {
				Console.Write ("Type filename to save if you want: ");
				filename = Console.ReadLine ();
			}
			if (filename.Length > 0) {
				var music = new SmfMusic ();
				var track = new SmfTrack ();
				foreach (var e in dump.Results) {
					if (e.SysEx != null)
						track.Events.Add (new SmfEvent (e.Timestamp, new SmfMessage (0xF0, 0, 0, e.SysEx)));
					else
						track.Events.Add (new SmfEvent (e.Timestamp, new SmfMessage (e.Message.Value)));
				}
				music.Tracks.Add (track);
				using (var f = File.Create (filename))
					new SmfWriter (f).WriteMusic (music);
			}
		}
	}

	public class BulkDump
	{
		public BulkDump ()
		{
			Interval = TimeSpan.FromMilliseconds (500);
		}

		static readonly byte [] sc88all = new byte [] {
			0xF0, 0x41, 0x10, 0x42, 0x11, // dev/cmd
			// addr
			0x0C, 0x00, 0x00,
			// size
			0x00, 0x00, 0x00,
			// chksum/EOX
			0x74, 0xF7 };
		static readonly byte [] sc88tone = new byte [] {
			0xF0, 0x41, 0x10, 0x42, 0x11, // dev/cmd
			// addr
			0x0C, 0x00, 0x01,
			// size
			0x00, 0x00, 0x00,
			// chksum/EOX
			0x73, 0xF7 };

		byte [] sysex = sc88tone;

		public void SetSysEx (byte [] data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			sysex = data;
		}

		MidiInput input_device;

		public void Start (int indev, int outdev)
		{
			using (var output = MidiDeviceManager.OpenOutput (outdev))
				output.WriteSysEx (0, sysex);
			input_device = MidiDeviceManager.OpenInput (indev);
			new Action (delegate {
				try {
					Loop ();
				} catch (Exception ex) {
					Console.WriteLine ("ERROR INSIDE THE LOOP: " + ex);
				}
				wait_handle.Set ();
				}).BeginInvoke (null, null);
		}

		public void Stop ()
		{
			loop = false;
			wait_handle.WaitOne ();
			input_device.Close ();
		}

		public TimeSpan Interval { get; set; }

		ManualResetEvent wait_handle = new ManualResetEvent (false);
		bool loop = true;
		List<MidiEvent> results = new List<MidiEvent> ();

		public IList<MidiEvent> Results { get { return results; } }

		void Loop ()
		{
			byte [] buf = new byte [0x10000];
			while (loop) {
				Thread.Sleep ((int) Interval.TotalMilliseconds); // some interval is required to stably receive messages...
				int size = input_device.Read (buf, 0, buf.Length);
//for (int i = 0; i < size; i++) Console.Write ("{0:X02} ", buf [i]);
				foreach (var ev in MidiInput.Convert (buf, 0, size))
					results.Add (ev);
			}
		}
	}
}


