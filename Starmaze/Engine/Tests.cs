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
		public void EmptyActor(){
			Actor a = new Actor();
			var json = JsonConvert.SerializeObject(a,jset);
			Log.Message("{0}", json);
			var z = JsonConvert.DeserializeObject<Actor>(json);
			Assert.True(true);
		}
		[Test]
		public void ActorWithEmptyComponent(){
			Actor a = new Actor();
			Component b = new Component(a);
			a.Components.Add(b);
			var json = JsonConvert.SerializeObject(a,jset);
			Log.Message("{0}", json);
			var z = JsonConvert.DeserializeObject<Actor>(json);
			Assert.True(true);
		}
		[Test]
		public void EmptyBBox(){
			BBox a = new BBox(0.0,1.0,0.0,1.0);
			var json = JsonConvert.SerializeObject(a,jset);
			Log.Message("{0}", json);
			var z = JsonConvert.DeserializeObject<BBox>(json);
			Assert.True(true);
		}

	}
}