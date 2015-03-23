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
		private WaveFormat format;
		public Sound (int sampleRate = 44100, int channelCount = 2)
		{
			player = new WaveOutEvent();
			format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount);
			mixer = new MixingSampleProvider(format);
			mixer.ReadFully = true;
			player.Init(mixer);
			player.Play();
		}

		public void PlaySound(ISampleProvider input)
		{
			mixer.AddMixerInput(CorrectInput(input));
		}

		private ISampleProvider CorrectInput(ISampleProvider input){
			input = CorrectChannels(input);
			input = CorrectSampleRate(input);
			return input;
		}

		private ISampleProvider CorrectChannels(ISampleProvider input){
			if (input.WaveFormat.Channels == mixer.WaveFormat.Channels)
			{
				return input;
			}
			if (input.WaveFormat.Channels == 1 && mixer.WaveFormat.Channels == 2)
			{
				return new MonoToStereoSampleProvider(input);
			}
			throw new NotImplementedException("Not yet implemented this channel count conversion");
		}
		private ISampleProvider CorrectSampleRate(ISampleProvider input){		
			if (input.WaveFormat.SampleRate == format.SampleRate) {
				//todo: fix this shit
				return input;
			}
			throw new Exception("NOT THE RIGHT SAMPLE RATE");
		}
	}
}

