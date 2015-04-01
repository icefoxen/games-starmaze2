using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpenTK;
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
			Log.LogToConsole = true;
			// Create a dummy GameWindow, which creates an OpenGL context so that if necessary a test
			// can load a shader, model, whatever
			if (g == null) {
				g = new GameWindow();
			}
			if (!Resources.IsInitialized) {
				Resources.Init();
			}
		}

		[Test]
		public void LifeAssetConverterTest()
		{
			var dummy = new Actor();
			var a = new Life(dummy, 20, 30, 0.8, 2);
			var json = SaveLoad.Save(a);
			Log.Message("Saved life: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded life: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void EnergyAssetConverterTest()
		{
			var dummy = new Actor();
			var a = new Energy(dummy, 50, 5.3);
			var json = SaveLoad.Save(a);
			Log.Message("Saved energy: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded energy: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void BodyAssetConverterTest()
		{
			var dummy = new Actor();
			var a = new Body(dummy);
			a.Position = new Vector2d(34, 5);
			var json = SaveLoad.Save(a);
			Log.Message("Saved body: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded body: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void InputControllerAssetConverterTest()
		{
			var dummy = new Actor();
			var a = new InputController(dummy);
			var json = SaveLoad.Save(a);
			Log.Message("Saved input controller: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded input controllet: {0}", z);
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
			var body = new Body(a);
			a.Body = body;
			body.AddGeom(new BoxGeom(new BBox(-5, -15, 5, 5)));
			a.AddComponent(new InputController(a));
			a.AddComponent(new Life(a, 15));
			a.AddComponent(new TimedLife(a, 15));
			a.AddComponent(new Gun(a));

			var tex = Resources.TheResources.GetTexture("PlayerAssetAnimationTestSpriteSheetv3");
			var atlas = new TextureAtlas(tex, 16, 1);
			var anim = new Animation(10, 0.2);
			var anim2 = new Animation(2, 0.2);
			a.AddComponent(new SpriteRenderState(a, atlas, new Animation[] { anim, anim2 }, scale: new Vector2(3f, 3f)));


			var json = SaveLoad.Save(a);
			Log.Message("Saved non-empty actor: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded non-empty actor: {0}", z);
			Assert.True(true);
		}
	}
}
