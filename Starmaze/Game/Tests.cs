using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using OpenTK;
using OpenTK.Graphics;
using Starmaze.Engine;



namespace Starmaze.Game
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
				Resources.Init();
			}
			jset = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
		}

		[Test]
		public void LifeSerialTest()
		{
			var dummy = new Actor();
			var a = new Life(dummy, 10);
			var json = JsonConvert.SerializeObject(a, jset);
			Log.Message("Life: {0}", json);
			var z = JsonConvert.DeserializeObject(json);
			Log.Message("Type of result: {0}", z.GetType());
			var j = Newtonsoft.Json.Linq.JObject.Parse(json);

			Log.Message("Life: {0}", z);
			Assert.True(true);
		}
	}
}