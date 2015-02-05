using System;
using System.Collections.Generic;
using OpenTK;
using NUnit.Framework;

namespace Starmaze.Engine
{
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
}