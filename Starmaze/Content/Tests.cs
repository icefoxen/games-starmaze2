using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpenTK;
using OpenTK.Graphics;
using Starmaze;
using Starmaze.Engine;
using Starmaze.Game;

namespace Starmaze.Content
{
	[TestFixture]
	public class SerializationTests
	{
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
		}


		[Test]
		public void BeginningsPowerConverterTest()
		{
			var a = new PowerSet();
			a.AddPower(new Beginnings.BeginningsPower());
			var json = SaveLoad.Save(a);
			Log.Message("Saved power set: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded power set: {0}", z);
			Assert.True(true);
		}

	}

}