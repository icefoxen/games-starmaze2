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
	public class SerializationTests{
		JsonSerializerSettings jset;
		[SetUp]
		public void Prep(){
			jset = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects };
		}

		[Test]
		public void EmptyActorSerialTest(){
			Actor a = new Actor();
			var json = JsonConvert.SerializeObject(a,jset);
			Log.Message("{0}", json);
			var z = JsonConvert.DeserializeObject<Actor>(json);
			Assert.True(true);
		}
		[Test]
		public void ActorWithEmptyComponentSerialTest(){
			Actor a = new Actor();
			Component b = new Component(a);
			a.Components.Add(b);
			var json = JsonConvert.SerializeObject(a,jset);
			Log.Message("{0}", json);
			var z = JsonConvert.DeserializeObject<Actor>(json);
			Assert.True(true);
		}
		[Test]
		public void BBoxSerialTest(){
			BBox a = new BBox(0.0,1.0,0.0,1.0);
			var json = JsonConvert.SerializeObject(a,jset);
			Log.Message("{0}", json);
			var z = JsonConvert.DeserializeObject<BBox>(json);
			Assert.True(true);
		}

		[Test]
		public void LineSerialTest(){
			Line a = new Line(0.0,1.0,0.0,1.0);
			var json = JsonConvert.SerializeObject(a,jset);
			Log.Message("{0}", json);
			var z = JsonConvert.DeserializeObject<Line>(json);
			Assert.True(true);
		}

		[Test]
		public void BoxGeomSerialTest(){
			BoxGeom a = new BoxGeom(new BBox(0.0,1.0,0.0,1.0));
			var json = JsonConvert.SerializeObject(a,jset);
			Log.Message("{0}", json);
			var z = JsonConvert.DeserializeObject<BoxGeom>(json);
			Assert.True(true);
		}
		//
		//Terrain.cs
		//
		[Test]
		public void BoxBlockSerialTest(){
			BoxBlock a = new BoxBlock(new BBox(0.0,1.0,2.0,2.0),Color4.AliceBlue);
			var json = JsonConvert.SerializeObject(a,jset);
			Log.Message("{0}", json);
			var z = JsonConvert.DeserializeObject<BoxBlock>(json);
			Assert.True(true);
		}

		//
		//Worldmap.cs
		//
		[Test]
		public void EmptyRoomSerialTest(){
			Room a = new Room("", new Actor[]{new Actor()});
			var json = JsonConvert.SerializeObject(a,jset);
			Log.Message("{0}", json);
			var z = JsonConvert.DeserializeObject<Room>(json);
			Assert.True(true);
		}
		[Test]
		public void RoomCreationTest(){
			var actors1 = new Actor[]{
				new Actor(),
				new Actor(),
				new Actor(),
				new Actor()
			};
			var a = new Room("TestRoom1",actors1);
			var json = JsonConvert.SerializeObject(a,jset);
			Log.Message("{0}", json);
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
	}
}