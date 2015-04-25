using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpenTK;
using OpenTK.Graphics;
using Starmaze.Engine;

namespace Starmaze.Game
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
		public void LifeAssetConverterTest()
		{
			var a = new Life(20, 30, 0.8, 2);
			var json = SaveLoad.Save(a);
			Log.Message("Saved life: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded life: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void EnergyAssetConverterTest()
		{
			var a = new Energy(50, 5.3);
			var json = SaveLoad.Save(a);
			Log.Message("Saved energy: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded energy: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void BodyAssetConverterTest()
		{
			var a = new Body();
			a.Position = new Vector2(34, 5);
			var json = SaveLoad.Save(a);
			Log.Message("Saved body: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded body: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void InputControllerAssetConverterTest()
		{
			var a = new InputController();
			var json = SaveLoad.Save(a);
			Log.Message("Saved input controller: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded input controllet: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void PowerSetAssetConverterTest()
		{
			var a = new PowerSet();
			var json = SaveLoad.Save(a);
			Log.Message("Saved power set: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded power set: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void EmptyActorAssetConverterTest()
		{
			var a = new Actor();
			var json = SaveLoad.Save(a);
			Log.Message("Saved empty actor: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded empty actor: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void ActorAssetConverterTest()
		{
			var a = new Actor();
			var body = new Body();
			a.AddComponent(body);
			//body.AddGeom(new BoxGeom(new BBox(-5, -15, 5, 5)));
			a.AddComponent(new InputController());
			a.AddComponent(new Life(15));
			a.AddComponent(new TimedLife(15));
			a.AddComponent(new Gun());

			var tex = Resources.TheResources.GetTexture("PlayerAssetAnimationTestSpriteSheetv3");
			var atlas = new TextureAtlas(tex, 16, 1);
			var anim = new Animation(10, 0.2);
			var anim2 = new Animation(2, 0.2);
			Log.Message("Resources initialized: {0}", Resources.IsInitialized);
			a.AddComponent(new SpriteRenderState(atlas, new Animation[] { anim, anim2 }, scale: new Vector2(3f, 3f)));


			var json = SaveLoad.Save(a);
			Log.Message("Saved non-empty actor: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded non-empty actor: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void RoomAssetConverterTest()
		{
			var actors = new Actor[] {
				new Actor(),
				new Actor(),
			};
			var a = new Room("TestRoom1", actors);

			var json = SaveLoad.Save(a);
			Log.Message("Saved room: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded room: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void ZoneAssetConverterTest()
		{
			var zone = new Zone("TestZone");
			var actors1 = new Actor[] {
				new Actor(),
				new Actor(),
			};
			var actors2 = new Actor[] {
				new Actor(),
				new Actor(),
			};
			var room1 = new Room("TestRoom1", actors1);
			var room2 = new Room("TestRoom2", actors2);
			zone.AddRoom(room1);
			zone.AddRoom(room2);

			var a = zone;

			var json = SaveLoad.Save(a);
			Log.Message("Saved zone: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded zone: {0}", z);
			Assert.True(true);
		}
	}
}
