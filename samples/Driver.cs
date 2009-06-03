using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Commons.Music.Midi;
#if Moonlight
using MidiOutput = System.IntPtr;
using System.Windows.Threading;
#else
using PortMidiSharp;
using Timer = System.Timers.Timer;
#endif

namespace Commons.Music.Midi.Player
{
#if !Moonlight
	public class Driver
	{
		public static void Main (string [] args)
		{
			var output = MidiDeviceManager.OpenOutput (MidiDeviceManager.DefaultOutputDeviceID);

			foreach (var arg in args) {
				var parser = new SmfReader (File.OpenRead (arg));
				parser.Parse ();
#if false
/* // test reader/writer sanity
				using (var outfile = File.Create ("testtest.mid")) {
					var data = parser.Music;
					var gen = new SmfWriter (outfile);
					gen.WriteHeader (data.Format, (short)data.Tracks.Count, data.DeltaTimeSpec);
					foreach (var tr in data.Tracks)
						gen.WriteTrack (tr);
				}
*/
// test merger/splitter
/*
				var merged = SmfTrackMerger.Merge (parser.Music);
//				var result = merged;
				var result = SmfTrackSplitter.Split (merged.Tracks [0].Events, parser.Music.DeltaTimeSpec);
				using (var outfile = File.Create ("testtest.mid")) {
					var gen = new SmfWriter (outfile);
					gen.DisableRunningStatus = true;
					gen.WriteHeader (result.Format, (short)result.Tracks.Count, result.DeltaTimeSpec);
					foreach (var tr in result.Tracks)
						gen.WriteTrack (tr);
				}
*/
#else
				var player = new PortMidiPlayer (output, parser.Music);
				player.StartLoop ();
				player.PlayAsync ();
				Console.WriteLine ("empty line to quit, P to pause and resume");
				while (true) {
					string line = Console.ReadLine ();
					if (line == "P") {
						if (player.State == PlayerState.Playing)
							player.PauseAsync ();
						else
							player.PlayAsync ();
					}
					else if (line == "") {
						player.Dispose ();
						break;
					}
					else
						Console.WriteLine ("what do you mean by '{0}' ?", line);
				}
#endif
			}
		}
	}
#endif
}

