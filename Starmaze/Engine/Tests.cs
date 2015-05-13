using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using OpenTK;
using OpenTK.Graphics;

//using Starmaze;
using Starmaze.Game;

namespace Starmaze.Engine
{
	/*
	[TestFixture]
	public class EngineTests{
		[SetUp]
		public void Prep(){
			Log.Message("Test prep");
		}

		[Test]
		public void PassTest(){
			Assert.True(true);
		}

		[Test]
		public void FailTest(){
			Assert.True(false);
		}
	}
	*/
	[TestFixture]
	public class SoundTests
	{
		Sound s;
		JsonSerializerSettings jset;
		GameWindow g;

		[SetUp]
		public void Prep()
		{
			Log.Init();
			Log.LogToConsole = true;
			// Create a dummy GameWindow, which creates an OpenGL context so that if necessary a test
			// can load a shader, model, whatever
			if (g == null) {
				g = new GameWindow();
			}
			if (!Resources.IsInitialized) {
				Resources.Init(new GameOptions());
			}
			s = new Sound();
		}

		[Test]
		public void PlaySound()
		{
			var r = Resources.TheResources.GetSound("Powers_Air_Wave_Large.wav");
			s.PlaySound(r);
			Assert.True(true);
		}

		[Test]
		public void PlayTwoSounds()
		{
			s.PlaySound(Resources.TheResources.GetSound("Powers_Air_Wave_Large.wav"));
			s.PlaySound(Resources.TheResources.GetSound("Powers_Air_Wave_Small.wav"));
			Assert.True(true);
		}

		[Test]
		public void PlayTwoSoundsWithDelay()
		{
			s.PlaySound(Resources.TheResources.GetSound("Powers_Air_Wave_Large.wav"));
			System.Threading.Thread.Sleep(500);
			s.PlaySound(Resources.TheResources.GetSound("Powers_Air_Wave_Small.wav"));
			Assert.True(true);
		}


		[Test]
		public void PlayOneSoundTwice()
		{
			s.PlaySound(Resources.TheResources.GetSound("Powers_Air_Wave_Large.wav"));
			System.Threading.Thread.Sleep(500);
			s.PlaySound(Resources.TheResources.GetSound("Powers_Air_Wave_Large.wav"));
			Assert.True(true);
		}

		[Test]
		public void PlayBrokenSound()
		{
			s.PlaySound(Resources.TheResources.GetSound("ResampleTester.wav"));
			Assert.True(true);
		}

		[Test]
		public void PlayMissingSound()
		{
			s.PlaySound(Resources.TheResources.GetSound("JOZXYQK.wav"));
			Assert.True(true);
		}


		[Test]
		public void PlaySoundWithVolume()
		{
			s.SetVolume(0.5f);
			s.PlaySound(Resources.TheResources.GetSound("Powers_Air_Wave_Large.wav"));
			Assert.True(true);
		}
	}

	[TestFixture]
	public class SerializationTests
	{
		JsonSerializerSettings jset;
		GameWindow g;

		[SetUp]
		public void Prep()
		{
			Log.LogToConsole = true;
			// Create a dummy GameWindow, which creates an OpenGL context so that if necessary a test
			// can load a shader, model, whatever
			if (g == null) {
				g = new GameWindow();
			}
			if (!Resources.IsInitialized) {
				Resources.Init(new GameOptions());
			}
			jset = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects };
		}


		[Test]
		public void GameOptionSerialize()
		{
			GameOptions a = new GameOptions();
			var j = JsonConvert.SerializeObject(a, jset);
			Log.Message("Serialized game options: {0}", j);
			Assert.True(true);
		}
	}
}