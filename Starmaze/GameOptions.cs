using System;
using System.IO;
using System.Linq;
using Starmaze.Engine;
using Newtonsoft.Json;
using OpenTK;

namespace Starmaze
{
	public class GameOptions
	{
		public int ResolutionW;
		public int ResolutionH;

		[JsonIgnore]
		public double AspectRatio { 
			get { 
				return ((double)ResolutionW) / ((double)ResolutionH);
			}
		}

		public VSyncMode Vsync;
		public GameWindowFlags WindowMode;
		public KeyboardBinding KeyBinding;
		public float SoundVolume;
		public int SoundSampleRate;
		public int SoundChannels;

		public GameOptions()
		{
			ResolutionW = 1024;
			ResolutionH = 768;
			Vsync = VSyncMode.On;
			WindowMode = GameWindowFlags.Default;
			KeyBinding = new KeyboardBinding(new KeyConfig());

			SoundVolume = 100f;
			SoundSampleRate = 44100;
			SoundChannels = 2;
		}

		/// <summary>
		/// Loads a GameOptions object from a file.
		/// The file must be in the same directory as the executable.
		/// Uses json.net instead of our schmancy homebrew object-loader, and you know, that's *fine*.  It's okay.
		/// In this simple case, it works great.
		/// </summary>
		/// <returns>The options file.</returns>
		/// <param name="fileName">File name.</param>
		public static GameOptions OptionsFromFile(string fileName = "settings.cfg")
		{
			var fullFilePath = Util.BasePath(fileName);
			if (!File.Exists(fullFilePath)) {
				Log.Message("Unable to open settings file, loading default settings.");
				return new GameOptions();
			} else {
				string json = File.ReadAllText(fullFilePath);
				GameOptions options = JsonConvert.DeserializeObject<GameOptions>(json);
				Log.Message("Loading game options from settings file: {0}", json);
				return options;
			}

		}

		public static void OptionsToFile(GameOptions options, string fileName = "settings.cfg")
		{
			var fullFilePath = Util.BasePath(fileName);
			var optionString = JsonConvert.SerializeObject(options);
			File.WriteAllText(fullFilePath, optionString);
		}
	}
}

