using System;
using System.IO;
using System.Threading;
using NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Mixer;
using System.Linq;
using System.Collections.Generic;
namespace Starmaze.Engine
{
	public class Sound
	{
		// See http://mark-dot-net.blogspot.co.uk/2014/02/fire-and-forget-audio-playback-with.html
		// Also see http://channel9.msdn.com/coding4fun/articles/Skype-Voice-Changer for fx
		private readonly IWavePlayer player;
		private readonly MixingSampleProvider mixer;
		private WaveFormat format;
		//XXX: The current volume implementation may cause problems with long sounds, like music.
		public float Volume{get;set;}
		public Sound (int sampleRate = 44100, int channelCount = 2, float volumeIn = 1.0f)
		{
			player = new WaveOutEvent();
			format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount);
			mixer = new MixingSampleProvider(format);
			mixer.ReadFully = true;
			player.Init(mixer);
			player.Play();
			Volume = volumeIn;
		}

		public void PlaySound(CachedSound input)
		{
			var samples = new CachedSoundSampleProvider(input);
			var c = CorrectInput((ISampleProvider) samples);
			//var v = new VolumeSampleProvider(CorrectInput(input));
			var v = new VolumeSampleProvider(c);
			v.Volume = Volume;
			mixer.AddMixerInput(v);
		}



		private ISampleProvider CorrectInput(ISampleProvider input){
			input = CorrectChannels(input);
			//input = CorrectSampleRate(input);
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
		}/*
		private ISampleProvider CorrectSampleRate(ISampleProvider input){		
			var inputSampleRate = input.WaveFormat.SampleRate;
			var mixerSampleRate = format.SampleRate;

			if (inputSampleRate == mixerSampleRate) {
				//todo: fix this shit
				return input;
			}
			throw new Exception(string.Format("Mixer received incorrect sample rate, wanted {0}, got {1}", inputSampleRate, mixerSampleRate));
		}*/
	}

	public class CachedSound
	{
		public float[] AudioData { get; private set; }
		public WaveFormat WaveFormat { get; private set; }
		public CachedSound(string audioFileName)
		{
			using (var audioFileReader = new AudioFileReader(audioFileName))
			{
				// TODO: could add resampling in here if required
				WaveFormat = audioFileReader.WaveFormat;
				var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
				var readBuffer= new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
				int samplesRead;
				while((samplesRead = audioFileReader.Read(readBuffer,0,readBuffer.Length)) > 0)
				{
					wholeFile.AddRange(readBuffer.Take(samplesRead));
				}
				AudioData = wholeFile.ToArray();
			}
		}
	}

	class CachedSoundSampleProvider : ISampleProvider
	{
		private readonly CachedSound cachedSound;
		private long position;

		public CachedSoundSampleProvider(CachedSound cachedSound)
		{
			this.cachedSound = cachedSound;
		}

		public int Read(float[] buffer, int offset, int count)
		{
			var availableSamples = cachedSound.AudioData.Length - position;
			var samplesToCopy = Math.Min(availableSamples, count);
			Array.Copy(cachedSound.AudioData, position, buffer, offset, samplesToCopy);
			position += samplesToCopy;
			return (int)samplesToCopy;
		}

		public WaveFormat WaveFormat { get { return cachedSound.WaveFormat; } }
	}


}

