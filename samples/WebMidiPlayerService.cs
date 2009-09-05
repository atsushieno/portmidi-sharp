using System;
using System.ServiceModel;
using PortMidiSharp;

namespace Commons.Music.Midi.Player
{
	[ServiceContract (Namespace = "")]
	public interface IMidiDeviceContract
	{
		[OperationContract]
		void ProcessMessage (int msg);

		[OperationContract]
		void ProcessSysExMessage (byte [] data);
	}

	public class MidiDeviceHostDriver
	{
		public static void Main (string [] args)
		{
			var host = new ServiceHost (new MidiDeviceService (
				MidiDeviceManager.OpenOutput (MidiDeviceManager.DefaultOutputDeviceID), true));
			host.AddServiceEndpoint (typeof (IMidiDeviceContract),					new BasicHttpBinding (),
				"http://localhost:9090");
			host.Open ();
			Console.WriteLine ("type [CR] to stop...");
			Console.ReadLine ();
			host.Close ();
		}
	}

	[ServiceBehavior (InstanceContextMode = InstanceContextMode.Single)]
	public class MidiDeviceService : IMidiDeviceContract, IDisposable
	{
		MidiOutput output;
		bool dispose_device;

		public MidiDeviceService (MidiOutput output, bool disposeDevice)
		{
			this.output = output;
			dispose_device = disposeDevice;
		}

		public void ProcessMessage (int msg)
		{
			var m = new SmfMessage (msg);
			output.Write (0, new MidiMessage (m.StatusByte, m.Msb, m.Lsb));
		}

		public void ProcessSysExMessage (byte [] data)
		{
			WriteSysEx (0xF0, data);
		}


		void WriteSysEx (byte status, byte [] sysex)
		{
			var buf = new byte [sysex.Length + 1];
			buf [0] = status;
			Array.Copy (sysex, 0, buf, 1, buf.Length - 1);
			output.WriteSysEx (0, buf);
		}

		public void Dispose ()
		{
			if (dispose_device)
				output.Close ();
		}
	}
}

