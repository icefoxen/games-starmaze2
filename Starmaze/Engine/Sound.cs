using System;
using System.IO;
using System.Threading;
using NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Mixer;

namespace Starmaze.Engine
{
	public class Sound
	{
		// See http://mark-dot-net.blogspot.co.uk/2014/02/fire-and-forget-audio-playback-with.html
		// Also see http://channel9.msdn.com/coding4fun/articles/Skype-Voice-Changer for fx
		private readonly IWavePlayer player;
		private readonly MixingSampleProvider mixer;

		public Sound (int sampleRate = 44100, int channelCount = 2)
		{
			player = new WaveOutEvent();
			mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount));
			mixer.ReadFully = true;
			player.Init(mixer);
			player.Play();
		}

		public void PlaySound(ISampleProvider input)
		{

			//AudioFileReader input = new AudioFileReader(filename);
			//WaveOut wav = new WaveOut();
			//wav.Init(input);

			//mixer.AddMixerInput((IWaveProvider) input);
			mixer.AddMixerInput( input);
		}

			
	}
}

