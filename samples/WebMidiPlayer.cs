using System;
using System.ServiceModel;

namespace Commons.Music.Midi.Player
{
	public class WebMidiPlayer : MidiPlayer
	{
		IMidiDeviceClient client;
		public WebMidiPlayer (Uri uri, SmfMusic music)
			: base (music)
		{
			client = ChannelFactory<IMidiDeviceClient>.CreateChannel (new BasicHttpBinding (), new EndpointAddress (uri));
			client.Open ();

			MessageReceived += delegate (SmfMessage msg) {
				switch (msg.StatusByte) {
				case 0xF0:
				case 0xF7:
					client.ProcessSysExMessage (msg.Data);
					break;
				case 0xFF:
					// do nothing
					break;
				default:
					client.ProcessMessage (msg.Value);
					break;
				}
			};
		}
	}

	[ServiceContract (Namespace = "")]
	public interface IMidiDeviceContract
	{
		[OperationContract]
		void ProcessMessage (int msg);

		[OperationContract]
		void ProcessSysExMessage (byte [] data);

		[OperationContract (AsyncPattern = true)]
		IAsyncResult BeginProcessMessage (int msg, AsyncCallback callback, object state);

		void EndProcessMessage (IAsyncResult result);

		[OperationContract (AsyncPattern = true)]
		IAsyncResult BeginProcessSysExMessage (byte [] data, AsyncCallback callback, object state);

		void EndProcessSysExMessage (IAsyncResult result);
	}

	public interface IMidiDeviceClient : IMidiDeviceContract, IClientChannel
	{
	}
}

