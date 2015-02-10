using System;
using System.IO;
using System.Threading;
using NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Starmaze.Engine
{
	public class Sound
	{
		// See http://mark-dot-net.blogspot.co.uk/2014/02/fire-and-forget-audio-playback-with.html
		// Also see http://channel9.msdn.com/coding4fun/articles/Skype-Voice-Changer for fx
		public static void PlaySound()
		{
			var waveOut = new WaveOut();
			var source1 = new AudioFileReader("../sounds/Powerup.wav");
			var source2 = new AudioFileReader("../sounds/Powers_Air_Wave_Large.wav");
			var mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat);
			waveOut.Init(source2);
			waveOut.Play();
			Thread.Sleep(500);
			waveOut.Play();
			Thread.Sleep(500);

		}
	}
}

