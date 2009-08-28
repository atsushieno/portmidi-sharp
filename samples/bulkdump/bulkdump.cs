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

			new BulkDump ().Run (inId, outId);
		}
	}

	public class BulkDump
	{
		public void Run (int indev, int outdev)
		{
			var sysex = new byte [] {
				0xF0, 0x41, 0x10, 0x42, 0x11, // dev/cmd
				// addr
				0x0C, 0x00, 0x00,
				// size
				0x00, 0x00, 0x00,
				// chksum/EOX
				0, 0xF7 };
			int chksum = 0;
			for (int i = 5; i < 11; i++)
				chksum += sysex [i];
			sysex [sysex.Length - 2] = (byte) (0x80 - chksum % 0x80);
foreach (var bbb in sysex) Console.Write ("{0:X02} ", bbb);
			using (var output = MidiDeviceManager.OpenOutput (outdev))
				output.WriteSysEx (0, sysex);
			Console.WriteLine ("Sent dump operation. Type [CR] to stop receive.");
			var dev = MidiDeviceManager.OpenInput (indev);
			new Action (delegate {
				try {
					Loop (dev);
				} catch (Exception ex) {
					Console.WriteLine ("ERROR INSIDE THE LOOP: " + ex);
				}
				wait_handle.Set ();
				}).BeginInvoke (null, null);
			Console.ReadLine ();
			loop = false;
			wait_handle.WaitOne ();
			dev.Close ();
		}

		ManualResetEvent wait_handle = new ManualResetEvent (false);
		bool loop = true;
		List<MidiEvent> results = new List<MidiEvent> ();

		void Loop (MidiInput dev)
		{
			int idx = 0, idx2 = 0;
			byte [] buf = new byte [0x10000];
			while (loop) {
				Thread.Sleep (300); // some interval is required to stably receive messages...
				int size = dev.Read (buf, idx, buf.Length - idx);
// if (size > 0) {
// Console.WriteLine ("Read {0} bytes", size);
// for (int z = idx; z < idx + size; z++) Console.Write ("{0:X02} ", buf [z]);
// Console.WriteLine (); 
// }
				idx += size;
				if (size > 0 && (buf [idx2] != 0xF0 || size > 10)) {
// if (buf [idx2] != 0xF0) Console.WriteLine ("##### {0:X02} {1:X02}", buf [idx2], buf [idx2 + 1]); else Console.WriteLine ("$$$$$ {0}", size);
					foreach (var ev in MidiInput.Convert (buf, idx2, idx - idx2))
						results.Add (ev);
					idx2 = idx;
				}
			}
		}
	}
}


