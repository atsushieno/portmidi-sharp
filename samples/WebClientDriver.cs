// gmcs WebClientDriver.cs WebMidiPlayer.cs SMF.cs MidiPlayer.cs MidiMachine.cs -pkg:wcf
using System;
using System.IO;
using System.ServiceModel;
using Commons.Music.Midi;

namespace Commons.Music.Midi.Player
{
	public class Driver
	{
		public static void Main (string [] args)
		{
			var reader = new SmfReader (File.OpenRead (args [0]));
			reader.Parse ();

			var player = new WebMidiPlayer (new Uri ("http://localhost:9090"), reader.Music);
			player.PlayAsync ();
			Console.WriteLine ("Type [CR] to stop...");
			Console.ReadLine ();
			player.Dispose ();
		}
	}
}

