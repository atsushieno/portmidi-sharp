using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using PortMidiSharp;

namespace Commons.Music.Midi
{
	public class Driver
	{
		public static void Main ()
		{
			int inId, outId;
			var a = new List<MidiDeviceInfo> (MidiDeviceManager.AllDevices);
			foreach (var dev in a)
				if (dev.IsInput)
					Console.WriteLine ("ID {0}: {1}", dev.ID, dev.Name);
			Console.WriteLine ("Type number to select MIDI In Device to use (type anything else to quit)");
			if (!int.TryParse (Console.ReadLine (), out inId))
				return;
			foreach (var dev in MidiDeviceManager.AllDevices)
				if (dev.IsOutput)
					Console.WriteLine ("ID {0}: {1}", dev.ID, dev.Name);
			Console.WriteLine ("Type number to select MIDI Out Device to use (type anything else to quit)");
			if (!int.TryParse (Console.ReadLine (), out outId))
				return;

			var dump = new BulkDump ();
			dump.Start (inId, outId);
			Console.WriteLine ("Type [CR] to stop receiving");
			Console.ReadLine ();
			dump.Stop ();

			Console.Write ("Type filename to save if you want: ");
			var s = Console.ReadLine ();
			if (s.Length > 0) {
				var music = new SmfMusic ();
				var track = new SmfTrack ();
				foreach (var e in dump.Results) {
					if (e.SysEx != null)
						track.Events.Add (new SmfEvent (e.Timestamp, new SmfMessage (0xF0, 0, 0, e.SysEx)));
					else
						track.Events.Add (new SmfEvent (e.Timestamp, new SmfMessage (e.Message.Value)));
				}
				music.Tracks.Add (track);
				using (var f = File.OpenWrite (s))
					new SmfWriter (f).WriteMusic (music);
			}
		}
	}

	public class BulkDump
	{
		public BulkDump ()
		{
			Interval = TimeSpan.FromMilliseconds (300);
		}

		static readonly byte [] sc88 = new byte [] {
			0xF0, 0x41, 0x10, 0x42, 0x11, // dev/cmd
			// addr
			0x0C, 0x00, 0x00,
			// size
			0x00, 0x00, 0x00,
			// chksum/EOX
			0x74, 0xF7 };

		byte [] sysex = sc88;

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
			int idx = 0;
			byte [] buf = new byte [0x10000];
			while (loop) {
				Thread.Sleep ((int) Interval.TotalMilliseconds); // some interval is required to stably receive messages...
				int size = input_device.Read (buf, idx, buf.Length - idx);
// if (size > 0) {
// Console.WriteLine ("Read {0} bytes", size);
// for (int z = idx; z < idx + size; z++) Console.Write ("{0:X02} ", buf [z]);
// Console.WriteLine (); 
// }
				idx += size;
				foreach (var ev in MidiInput.Convert (buf, idx, size))
					results.Add (ev);
			}
		}
	}
}


