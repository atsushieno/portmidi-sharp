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
#if true
				var player = new PortMidiSyncPlayer (output, parser.MusicData);
				player.PlayerLoop ();
#else
				var player = new PortMidiPlayer (output, parser.MusicData);
				player.PlayAsync ();
				Console.WriteLine ("empty line to quit, P to pause");
				while (true) {
					string line = Console.ReadLine ();
					if (line == "p") {
						if (player.State == PlayerState.Playing)
							player.PauseAsync ();
						else
							player.PlayAsync ();
					}
					else if (line == "") {
						player.Dispose ();
						break;
					}
				}
#endif
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
//Console.WriteLine ("----");
				foreach (var mev in track.Events) {
//Console.WriteLine ("[[ {0:X04} {1:X04} {2:X02} {3:X02} {4:X02} {5}]]", mev.DeltaTime, delta, mev.EventCode, mev.Arguments.Length > 0 ? mev.Arguments [0] : -1, mev.Arguments.Length > 1 ? mev.Arguments [1] : -1, mev.Definition.Name);
					var msg = new MidiMessage (
						mev.EventCode,
						mev.Arguments.Length > 0 ? (int) mev.Arguments [0] : 0,
						mev.Arguments.Length > 1 ? (int) mev.Arguments [1] : 0);
					delta += mev.DeltaTime;
					l.Add (new MidiEvent () { Timestamp = delta, Message = msg, SysEx = mev.GetRawArguments ()});
				}
				var last = new MidiEvent () { Timestamp = delta, Message = new MidiMessage (0, 0, 0) }; // dummy, has Value of 0.
				l.Add (last);
			}
			l.Sort (delegate (MidiEvent e1, MidiEvent e2) { return e1.Timestamp - e2.Timestamp; });
			var waitToNext = 0;
			for (int i = 0; i < l.Count - 1; i++) {
				if (l [i].Message.Value != 0 || l [i].SysEx != null) { // if non-dummy
					var me = l [i];
					var tmp = l [i + 1].Timestamp - l [i].Timestamp;
					me.Timestamp = waitToNext;
					waitToNext = tmp;// l [i + 1].Timestamp - l [i].Timestamp;
					l [i] = me;
				}
			}
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

		int event_idx = 0;

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
				if (event_idx == events.Count)
					break;
				HandleEvent (events [event_idx++]);
			}
		}

		int current_tempo = 500000; // dummy

		int GetDeltaTimeInMilliseconds (int deltaTime)
		{
			if (music.DeltaTimeSpec >= 0x80)
				throw new NotSupportedException ();
			return (int) (deltaTime * current_tempo / 1000 / music.DeltaTimeSpec);
		}

		string ToBinHexString (byte [] bytes)
		{
			string s = "";
			foreach (byte b in bytes)
				s += String.Format ("{0:X02} ", b);
			return s;
		}

		public virtual void HandleEvent (MidiEvent e)
		{
			if (e.Timestamp != 0) {
				var ms = GetDeltaTimeInMilliseconds (e.Timestamp);
//Console.WriteLine ("{0},{1:X} -> {2}", current_tempo, e.Timestamp, TimeSpan.FromMilliseconds (ms));
				Thread.Sleep (TimeSpan.FromMilliseconds (ms));
			}
			if ((e.Message.Value & 0xFF) == 0xFF && e.SysEx [0] == 0x51)
				current_tempo = (e.SysEx [1] << 16) + (e.SysEx [2] << 8) + e.SysEx [3];

			OnMessage (e);
			PlayDeltaTime += e.Timestamp;
		}

		protected virtual void OnMessage (MidiEvent e)
		{
//if (e.SysEx != null) { Console.Write("{0:X08}:", e.Message.Value); foreach (var b in e.SysEx) Console.Write ("{0:X02} ", b); Console.WriteLine (); }
			if ((e.Message.Value & 0xFF) == 0xF0)
				;//output.WriteSysEx (0, e.SysEx);
			else if ((e.Message.Value & 0xFF) == 0xF7)
				;//output.WriteSysEx (0, e.SysEx);
			else if ((e.Message.Value & 0xFF) == 0xFF)
				return; // meta. Nothing to send.
			else
				output.Write (0, e.Message);
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
			State = PlayerState.Stopped;
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
