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
				client.ProcessMessage (msg);
			};
		}
	}

	[ServiceContract (Namespace = "")]
	public interface IMidiDeviceContract
	{
		[OperationContract]
		void ProcessMessage (SmfMessage msg);
	}

	public interface IMidiDeviceClient : IMidiDeviceContract, IClientChannel
	{
	}
}

