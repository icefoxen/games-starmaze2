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
		//XXXX: The current volume implementation may cause problems with long sounds, like music.
		private float volume;
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
			var v = new VolumeSampleProvider(CorrectInput(input));
			v.Volume = volume;
			mixer.AddMixerInput(v);
		}
		public void SetVolume(float newVolume){
			volume = newVolume;
		}
		private ISampleProvider CorrectInput(ISampleProvider input){
			input = CorrectChannels(input);
			input = CorrectSampleRate(input);
			return input;
		}

		private ISampleProvider CorrectChannels(ISampleProvider input){
			var inputChannels = input.WaveFormat.Channels;
			var mixerChannels = mixer.WaveFormat.Channels;
			if (inputChannels == mixerChannels)
			{
				return input;
			}
			if (input.WaveFormat.Channels == 1 && mixer.WaveFormat.Channels == 2)
			{
				return new MonoToStereoSampleProvider(input);
			}
			throw new NotImplementedException(string.Format("Mixer channel count conversion not implemented, wanted {0}, got {1}", inputChannels, mixerChannels));
		}
		private ISampleProvider CorrectSampleRate(ISampleProvider input){		
			var inputSampleRate = input.WaveFormat.SampleRate;
			var mixerSampleRate = format.SampleRate;

			if (inputSampleRate == mixerSampleRate) {
				//todo: fix this shit
				return input;
			}
			throw new Exception(string.Format("Mixer received incorrect sample rate, wanted {0}, got {1}", inputSampleRate, mixerSampleRate));
		}
	}
}

