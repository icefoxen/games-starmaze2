using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using NUnit.Framework;
using Newtonsoft.Json;

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
				Resources.InitResources();
			}
			jset = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects };
		}

		[Test]
		public void VectorSerialTest()
		{
			Vector2d testvector = new Vector2d(4.0, 5.0);
			string testJSON = JsonConvert.SerializeObject(testvector, new OTKVector2dConverter());
			Vector2d vectorRT = JsonConvert.DeserializeObject<Vector2d>(testJSON);
			Assert.True(testvector == vectorRT);
		}

		// The Assert.True(true) in these is a little ingenuous, but these tests are mainly
		// there to see if the serialization runs at all, not whether it runs correctly.
		// If it hits an error before getting to the assert, the test is failed.
		[Test]
		public void EmptyActorSerialTest()
		{
			Actor a = new Actor();
			var json = JsonConvert.SerializeObject(a, jset);
			Log.Message("Empty actor: {0}", json);
			var z = JsonConvert.DeserializeObject<Actor>(json);

			Assert.True(true);
		}

		[Test]
		public void ActorWithEmptyComponentSerialTest()
		{
			Actor a = new Actor();
			Component b = new Component(a);
			a.Components.Add(b);
			var json = JsonConvert.SerializeObject(a, jset);
			Log.Message("Actor with component: {0}", json);
			var z = JsonConvert.DeserializeObject<Actor>(json);
			Assert.True(true);
		}

		[Test]
		public void BBoxSerialTest()
		{
			BBox a = new BBox(0.0, 1.0, 0.0, 1.0);
			var json = JsonConvert.SerializeObject(a, jset);
			Log.Message("BBox: {0}", json);
			var z = JsonConvert.DeserializeObject<BBox>(json);
			Assert.True(true);
		}

		[Test]
		public void LineSerialTest()
		{
			Line a = new Line(0.0, 1.0, 0.0, 1.0);
			var json = JsonConvert.SerializeObject(a, jset);
			Log.Message("Line: {0}", json);
			var z = JsonConvert.DeserializeObject<Line>(json);
			Assert.True(true);
		}

		[Test]
		public void BoxGeomSerialTest()
		{
			BoxGeom a = new BoxGeom(new BBox(0.0, 1.0, 0.0, 1.0));
			var json = JsonConvert.SerializeObject(a, jset);
			Log.Message("Box geom: {0}", json);
			var z = JsonConvert.DeserializeObject<BoxGeom>(json);
			Assert.True(true);
		}
		//
		//Terrain.cs
		//
		[Test]
		public void BoxBlockSerialTest()
		{

			BoxBlock a = new BoxBlock(new BBox(0.0, 1.0, 2.0, 2.0), Color4.AliceBlue);
			var json = JsonConvert.SerializeObject(a, jset);
			Log.Message("Box block: {0}", json);
			var z = JsonConvert.DeserializeObject<BoxBlock>(json);
			var postJson = JsonConvert.SerializeObject(z, jset);
			Log.Message("Box block: {0}", postJson);

			Assert.True(a.Body.Position==z.Body.Position);
			Assert.True(true);
		}

		//
		//Worldmap.cs
		//
		[Test]
		public void EmptyRoomSerialTest()
		{
			Room a = new Room("", new Actor[]{ new Actor() });
			var json = JsonConvert.SerializeObject(a, jset);
			Log.Message("Serialized empty room: {0}", json);
			var z = JsonConvert.DeserializeObject<Room>(json);
			Assert.True(true);
		}

		[Test]
		public void RoomCreationTest()
		{
			var actors1 = new Actor[] {
				new BoxBlock(new BBox(0.0, 1.0, 2.0, 2.0), Color4.AliceBlue),
				new Actor(),
				new Actor(),
				new Actor()
			};
			var a = new Room("TestRoom1", actors1);
			var json = JsonConvert.SerializeObject(a, jset);
			Log.Message("Serialized room: {0}", json);
			var z = JsonConvert.DeserializeObject<Room>(json);
			/*
			var actors1 = new Actor[] {
				new BoxBlock(new BBox(-40, -35, 40, -30), Color4.Blue),
				new BoxBlock(new BBox(-40, 30, 40, 35), Color4.Blue),
				new BoxBlock(new BBox(-45, -35, -40, 35), Color4.Blue),
				new BoxBlock(new BBox(40, -35, 45, 35), Color4.Blue),
			};
			var room1 = new Room("TestRoom1", actors1);*/
			Assert.True(true);
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