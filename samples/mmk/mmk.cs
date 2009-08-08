using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using PortMidiSharp;

namespace Commons.Music.Midi
{
	public class Mmk : Form
	{
		static readonly List<string> tone_list;

#if CHROMA_TONE
		public const bool ChromaTone = true;
#else
		public const bool ChromaTone = false;
#endif

		static Mmk ()
		{
			tone_list = new List<string> ();
			int n = 0;
			var chars = "\n".ToCharArray ();
			foreach (string s in new StreamReader (typeof (Mmk).Assembly.GetManifestResourceStream ("tonelist.txt")).ReadToEnd ().Split (chars, StringSplitOptions.RemoveEmptyEntries))
				tone_list.Add (n++ + ":" + s);
		}

		public static void Main ()
		{
			Application.Run (new Mmk ());
		}

		public Mmk ()
		{
			SetupMidiDevices ();

			this.Width = 420;
			this.Height = 300;
			this.Text = "MMK: MIDI Keyboard";

			SetupMenus ();

			var statusBar = new StatusBar ();
			Controls.Add (statusBar);

			SetupDeviceSelector ();
			SetupToneSelector ();
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
			cb.TabIndex = 2;
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
			output.Write (0, new MidiMessage (0xC0, 0, 0));
		}

		static readonly string [] tone_categories = {
			"0 Piano",
			"8 Chromatic Percussion",
			"16 Organ",
			"24 Guitar",
			"32 Bass",
			"40 Strings",
			"48 Ensemble",
			"56 Brass",
			"64 Reed",
			"72 Pipe",
			"80 Synth Lead",
			"88 Synth Pad",
			"96 Synth Effects",
			"104 Ethnic",
			"112 Percussive",
			"120 SFX"
			};

		void SetupToneSelector ()
		{
#if true
			var tone = new MenuItem ("&Tone");
			this.Menu.MenuItems.Add (tone);
			MenuItem sub = null;
			for (int i = 0; i < tone_list.Count; i++) {
				if (i % 8 == 0) {
					sub = new MenuItem (tone_categories [i / 8]);
					tone.MenuItems.Add (sub);
				}
				var mi = new MenuItem (tone_list [i]);
				mi.Tag = i;
				mi.Select += delegate {
					output.Write (0, new MidiMessage (0xC0, (int) mi.Tag, 0));
				};
				sub.MenuItems.Add (mi);
			}
#else
			ComboBox cb = new ComboBox ();
			cb.TabIndex = 3;
			cb.Location = new Point (10, 40);
			cb.Width = 200;
			cb.DropDownStyle = ComboBoxStyle.DropDownList;
			cb.DataSource = tone_list;
			cb.SelectedIndexChanged += delegate {
				output.Write (0, new MidiMessage (0xC0, cb.SelectedIndex, 0));
			};
			Controls.Add (cb);
#endif
		}

#if CHROMA_TONE
		static readonly string [] key_labels = {"c", "c+", "d", "d+", "e", "f", "f+", "g", "g+", "a", "a+", "b"};
#else
		static readonly string [] key_labels = {"c", "c+", "d", "d+", "e", "", "f", "f+", "g", "g+", "a", "a+", "b", ""};
#endif

		void SetupKeyboardLayout (KeyMap map)
		{
			keymap = map;

			int top = 70;

			// offset 4, 10, 18 are not mapped, so skip those numbers
			var hl = new List<Button> ();
			int labelStringIndex = key_labels.Length - 5;
			for (int i = 0; i < keymap.HighKeys.Length; i++) {
				var b = new NoteButton ();
				b.Text = key_labels [labelStringIndex % key_labels.Length];
				labelStringIndex++;
				if (!IsNotableIndex (i)) {
					b.Enabled = false;
					b.Visible = false;
				}
				b.Location = new Point (btSize / 2 + i * btSize / 2, i % 2 == 0 ? top : top + 5 + btSize);
				hl.Add (b);
				Controls.Add (b);
			}
			high_buttons = hl.ToArray ();
			var ll = new List<Button> ();
			labelStringIndex = key_labels.Length - 5;
			for (int i = 0; i < keymap.LowKeys.Length; i++) {
				var b = new NoteButton ();
				b.Text = key_labels [labelStringIndex % key_labels.Length];
				labelStringIndex++;
				if (!IsNotableIndex (i)) {
					b.Enabled = false;
					b.Visible = false;
				}
				b.Location = new Point (btSize + i * btSize / 2, i % 2 == 0 ? top + 10 + btSize * 2 : top + 15 + btSize * 3);
				ll.Add (b);
				Controls.Add (b);
			}
			low_buttons = ll.ToArray ();

			high_button_states = new bool [high_buttons.Length];
			low_button_states = new bool [low_buttons.Length];

			var tb = new TextBox ();
			tb.TabIndex = 0;
			tb.Location = new Point (10, 200);
			tb.TextChanged += delegate { tb.Text = String.Empty; };
			Controls.Add (tb);
			tb.KeyDown += delegate (object o, KeyEventArgs e) {
				ProcessKey (true, e);
			};
			tb.KeyUp += delegate (object o, KeyEventArgs e) {
				ProcessKey (false, e);
			};
		}

		static int btSize = 25;
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

		// check if the key is a notable key (in mmk).
		bool IsNotableIndex (int i)
		{
			if (ChromaTone)
				return true;

			switch (i) {
			case 4:
			case 10:
			case 18:
				return false;
			}
			return true;
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
				if (!IsNotableIndex (idx))
					return;

				if (idx >= 0)
					ProcessNodeKey (down, true, idx);
				else {
					idx = keymap.HighKeys.IndexOf ((char) key);
					if (!IsNotableIndex (idx))
						return;
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

			int nid = idx;
			if (!ChromaTone) {
				if (idx < 4)
					nid = idx;
				else if (idx < 10)
					nid = idx - 1;
				else if (idx < 18)
					nid = idx - 2;
				else
					nid = idx - 3;
			}

			int note;
			if (ChromaTone)
				note = octave * 12 - 4 + transpose + nid + (low ? 2 : 0);
			else
				note = (octave + (low ? 0 : 1)) * 12 - 4 + transpose + nid;

			if (0 <= note && note <= 128)
				output.Write (0, new MidiMessage (down ? 0x90 : 0x80, note, 100));
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

			public static readonly KeyMap JP106 = new KeyMap ("AZSXDCFVGBHNJMK\xbcL\xbe\xbb\xbf\xba\xe2\xdd", "1Q2W3E4R5T6Y7U8I9O0P\xbd\xc0\xde\xdb\xdc");

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

