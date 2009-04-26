using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using PortMidiSharp;

namespace Commons.MidiCompiler
{
	public class Driver
	{
		public static void Main (string [] args)
		{
			var output = MidiDeviceManager.OpenOutput (MidiDeviceManager.DefaultOutputDeviceID);
			foreach (var arg in args) {
				var parser = new SmfParser (File.OpenRead (arg));
				parser.Parse ();
				new PortMidiPlayer (output, parser.MusicData);
			}
		}
	}

	public enum PlayerState
	{
		Stopped,
		Playing,
		Paused
	}

	// Player implementation. Plays a MIDI song synchronously.
	public class PortMidiSyncPlayer : IDisposable
	{
		public PortMidiSyncPlayer (MidiOutput output, SmfMusicData music)
		{
			if (output == null)
				throw new ArgumentNullException ("output");
			if (music == null)
				throw new ArgumentNullException ("music");

			this.output = output;
			this.music = music;
			BuildSmfEvents (music);
		}

		MidiOutput output;
		SmfMusicData music;
		List<MidiEvent> events;
		ManualResetEvent pause_handle = new ManualResetEvent (true);
		bool pause, stop;

		public int PlayDeltaTime { get; set; }

		public void Dispose ()
		{
			output.Close ();
		}

		void BuildSmfEvents (SmfMusicData music)
		{
			var l = new List<MidiEvent> ();
			foreach (var track in music.Tracks) {
				int delta = 0;
				foreach (var mev in track.Events) {
					var msg = new MidiMessage (
						mev.Definition.EventType,
						mev.Arguments.Length > 0 ? (int) mev.Arguments [0] : 0,
						mev.Arguments.Length > 1 ? (int) mev.Arguments [1] : 0);
					l.Add (new MidiEvent () { Timestamp = delta, Message = msg, SysEx = mev.Definition.UseVariableArguments ? mev.Arguments : null });
					delta += mev.DeltaTime;
				}
				var last = new MidiEvent () { Timestamp = delta, Message = new MidiMessage (0, 0, 0) }; // dummy, has Value of 0.
				l.Add (last);
			}
			l.Sort (delegate (MidiEvent e1, MidiEvent e2) { return e1.Timestamp - e2.Timestamp; });
			for (int i = 0; i < l.Count - 1; i++)
				if (l [i].Message.Value != 0 || l [i].SysEx != null)
					l [i] = new MidiEvent () { Message = l [i].Message, Timestamp = l [i + 1].Timestamp - l [i].Timestamp};

			events = l;
		}

		public void Play ()
		{
			if (pause_handle != null) {
				pause_handle.Set ();
				pause_handle = null;
			}
		}

		public void Pause ()
		{
			pause = true;
		}

		public void PlayerLoop ()
		{
			while (true) {
				pause_handle.WaitOne ();
				if (stop)
					break;
				if (pause) {
					pause_handle.Reset ();
					pause = false;
					continue;
				}
			}
		}

		public void Stop ()
		{
			if (pause_handle != null)
				pause_handle.Set ();
			stop = true;
		}
	}

	// Provides asynchronous player control.
	public class PortMidiPlayer : IDisposable
	{
		PortMidiSyncPlayer player;
		Thread sync_player_thread;

		public PortMidiPlayer (MidiOutput output, SmfMusicData music)
		{
			player = new PortMidiSyncPlayer (output, music);
			sync_player_thread = new Thread (new ThreadStart (delegate { player.Play (); }));
		}

		public PlayerState State { get; set; }

		public void Dispose ()
		{
			player.Dispose ();
			if (sync_player_thread.ThreadState == ThreadState.Running)
				sync_player_thread.Abort ();
		}

		public void PlayAsync ()
		{
			switch (State) {
			case PlayerState.Playing:
				return; // do nothing
			case PlayerState.Paused:
				player.Play ();
				State = PlayerState.Playing;
				return;
			case PlayerState.Stopped:
				player.Play ();
				return;
			}
		}

		public void PauseAsync ()
		{
			switch (State) {
			case PlayerState.Playing:
				player.Pause ();
				State = PlayerState.Paused;
				return;
			default: // do nothing
				return;
			}
		}
	}
}
