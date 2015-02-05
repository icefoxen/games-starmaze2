using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using NUnit.Framework;
using Newtonsoft.Json;
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
		public void BoxBlock(){
			Log.Message("BoxBlock testing");
			Actor a = new BoxBlock(new BBox(-40, -35, 40, -30), Color4.Blue);
			//var json = JsonConvert.SerializeObject(a,jset);
			//Log.Message("{0}", json);
			Assert.True(true);
		}

	}
}