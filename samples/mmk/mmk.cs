using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using PortMidiSharp;

namespace Commons.Music.Midi
{
	public class Mmk : Form
	{
		public static void Main ()
		{
			Application.Run (new Mmk ());
		}

		public Mmk ()
		{
			SetupMidiDevices ();

			this.Width = 400;
			this.Height = 250;

			SetupMenus ();

			var statusBar = new StatusBar ();
			Controls.Add (statusBar);

			SetupDeviceSelector ();

			SetupKeyboardLayout (KeyMap.JP106); // FIXME: make it customizible
		}

		void SetupMidiDevices ()
		{
			Application.ApplicationExit += delegate {
				if (output != null)
					output.Dispose ();
			};

			if (MidiDeviceManager.DefaultOutputDeviceID < 0) {
				MessageBox.Show ("No MIDI device was found.");
				Application.Exit ();
				return;
			}

			foreach (var dev in MidiDeviceManager.AllDevices)
				if (dev.IsOutput)
					output_devices.Add (dev);
			SwitchToDevice (0);
		}

		void SetupMenus ()
		{
			var menu = new MainMenu ();
			var file = new MenuItem ("&File");
			menu.MenuItems.Add (file);
			var exit = new MenuItem ("&Exit", delegate { QuitApplication (); }, Shortcut.CtrlQ);
			file.MenuItems.Add (exit);
			this.Menu = menu;
		}

		void SetupDeviceSelector ()
		{
			ComboBox cb = new ComboBox ();
			cb.Location = new Point (10, 10);
			cb.Width = 200;
			cb.DropDownStyle = ComboBoxStyle.DropDownList;
			cb.DataSource = new List<string> (from dev in output_devices select dev.Name);
			cb.SelectedIndexChanged += delegate {
				try {
					this.Enabled = false;
					this.Cursor = Cursors.WaitCursor;
					if (cb.SelectedIndex < 0)
						return;
					SwitchToDevice (cb.SelectedIndex);
				} finally {
					this.Enabled = true;
					cb.Focus ();
					this.Cursor = Cursors.Default;
				}
			};
			Controls.Add (cb);
		}

		void SwitchToDevice (int deviceIndex)
		{
			if (output != null) {
				output.Dispose ();
				output = null;
			}
			output = MidiDeviceManager.OpenOutput (output_devices [deviceIndex].ID);
			output.Write (0, new MidiMessage (0xB0, 0, 0));
		}

		void SetupKeyboardLayout (KeyMap map)
		{
			keymap = map;

			// offset 4, 10, 18 are not mapped, so skip those numbers
			var hl = new List<Button> ();
			for (int i = 0, j = 0; i < keymap.HighKeys.Length; i++) {
				if (i == 4 || i == 10 || i == 18)
					continue;
				j++;
				var b = new NoteButton ();
				b.Location = new Point (btSize / 2 + i * btSize / 2, i % 2 == 0 ? 50 : 55 + btSize);
				hl.Add (b);
				Controls.Add (b);
			}
			high_buttons = hl.ToArray ();
			var ll = new List<Button> ();
			for (int i = 0, j = 0; i < keymap.HighKeys.Length; i++) {
				if (i == 4 || i == 10 || i == 18)
					continue;
				j++;
				var b = new NoteButton ();
				b.Location = new Point (btSize + i * btSize / 2, i % 2 == 0 ? 60 + btSize * 2 : 65 + btSize * 3);
				ll.Add (b);
				Controls.Add (b);
			}
			low_buttons = ll.ToArray ();

			high_button_states = new bool [high_buttons.Length];
			low_button_states = new bool [low_buttons.Length];

			KeyDown += delegate (object o, KeyEventArgs e) {
				ProcessKey (true, e);
			};
			KeyUp += delegate (object o, KeyEventArgs e) {
				ProcessKey (false, e);
			};
		}

		static int btSize = 20;
		Button [] high_buttons;
		Button [] low_buttons;
		bool [] high_button_states;
		bool [] low_button_states;

		class NoteButton : Button
		{
			public NoteButton ()
			{
				Width = Mmk.btSize;
				Height = Mmk.btSize;
				Enabled = false;
			}

			protected override void OnGotFocus (EventArgs e)
			{
				Form.ActiveForm.Focus ();
			}
		}

		void ProcessKey (bool down, KeyEventArgs e)
		{
			var key = e.KeyCode;
			switch (key) {
			case Keys.Up:
				if (octave < 7)
					octave++;
				break;
			case Keys.Down:
				if (octave > 0)
					octave--;
				break;
//			case Keys.Left:
//				transpose--;
//				break;
//			case Keys.Right:
//				transpose++;
//				break;
			default:
				var idx = keymap.LowKeys.IndexOf ((char) key);
				if (idx >= 0)
					ProcessNodeKey (down, true, idx);
				else {
					idx = keymap.HighKeys.IndexOf ((char) key);
					if (idx >= 0)
						ProcessNodeKey (down, false, idx);
					else
						return;
				}
				break;
			}
			e.Handled = true;
		}

		void ProcessNodeKey (bool down, bool low, int idx)
		{
			var fl = low ? low_button_states : high_button_states;
			if (fl [idx] == down)
				return; // no need to process repeated keys.

			var b = low ? low_buttons [idx] : high_buttons [idx];
			if (down)
				b.BackColor = Color.Gray;
			else
				b.BackColor = this.BackColor;
			fl [idx] = down;

			// FIXME: verify
			int note = (octave + (low ? 0 : 1)) * 12 - 4 + transpose + idx;

Console.WriteLine (note);

			if (0 <= note && note <= 128)
				output.Write (0, new MidiMessage (down ? 0x90 : 0x80, note, 100));

//			Console.WriteLine ("{0} {1} {2}", down, low, idx);
		}

		class KeyMap
		{
			// note that those arrays do not contain non-mapped notes: index at 4, 10, 18

			// keyboard map for JP106
			// [1][2][3][4][5][6][7][8][9][0][-][^][\]
			//  [Q][W][E][R][T][Y][U][I][O][P][@][{]
			//  [A][S][D][F][G][H][J][K][L][;][:][}]
			//   [Z][X][C][V][B][N][M][<][>][?][_]
			// [UP] - octave up
			// [DOWN] - octave down
			// [LEFT] - <del>transpose decrease</del>
			// [RIGHT] - <del>transpose increase</del>

			public static readonly KeyMap JP106 = new KeyMap ("AZSXCFVGBNJMK\xbcL\xbe\xbf\xba\xe2\xdd]", "1Q2WE4R5TY7U8I9OP\xbd\xc0\xde\xdb\xdc"); // 3, 6, 0 and D, H, + are not mapped.
			public static readonly KeyMap US = new KeyMap ("AZSXCFVGBNJMK\xbcL\xbe\xbf\xba\xe2\xdd]", "1Q2WE4R5TY7U8I9OP\xbd\xc0\xde\xdb\xdc"); // FIXME: get correct mapping

			public KeyMap (string lowKeys, string highKeys)
			{
				LowKeys = lowKeys;
				HighKeys = highKeys;
			}

			public readonly string LowKeys;
			public readonly string HighKeys;
		}

		MidiOutput output;
		int transpose;
		int octave = 4; // lowest
		List<MidiDeviceInfo> output_devices = new List<MidiDeviceInfo> ();
		KeyMap keymap;

		void QuitApplication ()
		{
			// possibly show dialog in case we support MML editor buffer.
			Application.Exit ();
		}
	}
}

