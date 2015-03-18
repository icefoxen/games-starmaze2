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
	public interface ISaveLoadable
	{
		void PostLoad();

		void PreSave();
	}

	public interface ISaveLoad<T> where T : ISaveLoadable
	{
		T Load(JObject json, object accessory);

		JObject Save(T thing);

		string[] Props { get; }
	}

	public class LifeSaver : ISaveLoad<Life>
	{
		public string[] Props { get; set; }

		public LifeSaver()
		{
			Props = new string[] {
				"CurrentLife",
				"MaxLife",
				"DamageAttenuation",
				"DamageReduction",
			};
		}

		public Life Load(JObject json, object accessory)
		{
			var act = accessory as Actor;
			Log.Assert(act != null, "Aiee!");
			var l = new Life(act, 0);
			var typ = l.GetType();
			foreach (var propName in Props) {
				var property = typ.GetProperty(propName);
				var loadedValue = json[propName].ToObject(property.PropertyType);
				Log.Message("Setting property {0} to {1}", property, loadedValue);
				property.SetValue(l, loadedValue);
			}
			l.PostLoad();
			return l;
		}

		public JObject Save(Life l)
		{
			Log.Assert(l != null, "Shouldn't be possible!");
			l.PreSave();
			var typ = l.GetType();
			var json = new JObject();
			/*
			foreach (var field in typ.GetFields()) {
				Log.Message("Field: {0}", field);
			}
			foreach (var prop in typ.GetProperties()) {
				Log.Message("Prop: {0}", prop);
			}
			*/
			foreach (var propName in Props) {
				//var field = typ.GetField(propName);
				//var val = field.GetValue(l);
				var property = typ.GetProperty(propName);
				var val = property.GetValue(l);
				Log.Message("Saving field {0}, value {1}", property, val);
				// TODO: We need to know what type val is and serialize it as well
				// This is just to test the overall shape.
				//json.Add(propName, JObject.FromObject(val));
				json[propName] = new JValue(val);
				var jsonVal = json[propName];
				Log.Assert(jsonVal != null, "Json missing value {0}", propName);
				Log.Message("Got from json: {0}", jsonVal);
				var loadedValue = jsonVal.ToObject(property.PropertyType);
				property.SetValue(l, loadedValue);
				//var loadedValue = json[propName].ToObject(field.FieldType);
				//field.SetValue(l, loadedValue);
			}
			json.Add("type", typ.ToString());
			return json;
		}
	}

	public class LifeConverter : Newtonsoft.Json.JsonConverter
	{
		public override bool CanRead { get { return true; } }

		public override bool CanWrite { get { return true; } }

		public string[] Props = new string[] {
			"CurrentLife",
			"MaxLife",
			"DamageAttenuation",
			"DamageReduction",
		};

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(Life);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var json = JObject.ReadFrom(reader);
			//var act = existingValue as Actor;
			//Log.Assert(act != null, "Aiee!");
			// XXX: Another hole to plug...
			Actor act = null;
			var l = new Life(act, 0);
			var typ = l.GetType();
			foreach (var propName in Props) {
				var property = typ.GetProperty(propName);
				var loadedValue = json[propName].ToObject(property.PropertyType);
				Log.Message("Setting property {0} to {1}", property, loadedValue);
				property.SetValue(l, loadedValue);
			}
			l.PostLoad();
			return l;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var l = value as Life;
			Log.Assert(l != null, "Shouldn't be possible!");
			l.PreSave();
			var typ = l.GetType();
			var json = new JObject();
			/*
			foreach (var field in typ.GetFields()) {
				Log.Message("Field: {0}", field);
			}
			foreach (var prop in typ.GetProperties()) {
				Log.Message("Prop: {0}", prop);
			}
			*/
			foreach (var propName in Props) {
				//var field = typ.GetField(propName);
				//var val = field.GetValue(l);
				var property = typ.GetProperty(propName);
				var val = property.GetValue(l);
				Log.Message("Saving field {0}, value {1}", property, val);
				// TODO: We need to know what type val is and serialize it as well
				// This is just to test the overall shape.
				//json.Add(propName, JObject.FromObject(val));
				json[propName] = new JValue(val);
				var jsonVal = json[propName];
				Log.Assert(jsonVal != null, "Json missing value {0}", propName);
				Log.Message("Got from json: {0}", jsonVal);
				var loadedValue = jsonVal.ToObject(property.PropertyType);
				property.SetValue(l, loadedValue);
				//var loadedValue = json[propName].ToObject(field.FieldType);
				//field.SetValue(l, loadedValue);
			}
			json.Add("$type", typ.ToString());
			json.WriteTo(writer);
		}
	}

	public class SaveLoadThing
	{
		Dictionary<Type, ISaveLoad<ISaveLoadable>> SLDict;

		public SaveLoadThing()
		{
			SLDict = new Dictionary<Type, ISaveLoad<ISaveLoadable>> {
				{typeof(Life), (ISaveLoad<ISaveLoadable>) new LifeSaver()}
			};
		}

		public JObject Save(ISaveLoadable o)
		{
			var typ = o.GetType();
			ISaveLoad<ISaveLoadable> saver;
			if (!SLDict.TryGetValue(typ, out saver)) {
				var msg = String.Format("Could not find saver for type {0}", typ);
				throw new Exception(msg);
			}
			return saver.Save(o);
		}

		public ISaveLoadable Load(JObject json, object accessory)
		{
			var typeName = json["type"].Value<string>();
			var typ = Type.GetType(typeName);
			ISaveLoad<ISaveLoadable> loader;
			if (!SLDict.TryGetValue(typ, out loader)) {
				var msg = String.Format("Could not find loader for type {0}", typeName);
				throw new Exception(msg);
			}
			return loader.Load(json, accessory);
		}
	}

	static class SaveLoad
	{
		public static Actor LoadActor(JObject json)
		{
			var act = new Actor();
			act.KeepOnRoomChange = json["keepOnRoomChange"].Value<bool>();
			foreach (var component in LoadComponents(act, json["components"].Value<JArray>())) {
				act.AddComponent(component);
			}
			return act;
		}

		public static JObject SaveActor(Actor a)
		{
			var json = new JObject {
				{ "type", a.GetType().ToString() },
				{ "keepOnRoomChange", a.KeepOnRoomChange },
				//{"renderState", a.RenderState},
				{ "components", SaveComponents(a.Components) }
			};
			return json;
		}

		static IEnumerable<Component> LoadComponents(Actor act, JArray json)
		{
			var l = new List<Component>();
			foreach (var obj in json) {
				l.Add(LoadComponent(act, obj.Value<JObject>()));
			}
			return l;
		}

		static JArray SaveComponents(IEnumerable<Component> components)
		{
			var json = new JArray();
			foreach (var component in components) {
				json.Add(SaveComponent(component));
			}
			return json;
		}

		public static Component LoadComponent(Actor act, JObject json)
		{
			JToken tok;
			var got = json.TryGetValue("type", out tok);
			if (!got) {
				var msg = String.Format("No type field when trying to load component: {0}", json);
				throw new JsonSerializationException(msg);
			}
			var typ = tok.Value<string>();
			Log.Message("Loading component of type: {0}", typ);
			// This is a little ugly, but it gets everything hard-coded in at compile time,
			// which is exactly what we want, semantically.  Also prevents irritating type
			// shenanigans when we try to put bunches of functions into a collection or such.
			// Yay brute force, I guess!
			switch (typ) {
				case "Starmaze.Game.Life":
					return LoadLife(act, json);
				case "Starmaze.Game.TimedLife":
					return LoadTimedLife(act, json);
				case "Starmaze.Game.Energy":
					return LoadEnergy(act, json);
				case "Starmaze.Game.InputController":
					return LoadInputController(act, json);
				case "Starmaze.Engine.Body":
					return LoadBody(act, json);
				case "Starmaze.Engine.RenderState":
					return LoadRenderState(act, json);
				case "Starmaze.Engine.ModelRenderState":
					return LoadModelRenderState(act, json);
				case "Starmaze.Engine.BillboardRenderState":
					return LoadBillboardRenderState(act, json);
				case "Starmaze.Engine.SpriteRenderState":
					return LoadSpriteRenderState(act, json);
				default:
					var msg = String.Format("Don't know how to load component type:: {0}", typ);
					throw new JsonSerializationException(msg);
			}
		}

		public static JObject SaveComponent(Component c)
		{
			var typ = c.GetType().ToString();
			/*
			var t = c.GetType();
			var props = t.GetProperties();
			foreach(var p in props) {
				var attrs = p.Attributes;
				var getter = p.GetMethod;

				var parms = getter.GetParameters();
				foreach(var parm in parms) {
					parm.
				}
			}
			*/
			Log.Message("Saving component of type: {0}", typ);
			// Using the 'is' keyword might be more auspicious?
			switch (typ) {
				case "Starmaze.Game.Life":
					{
						var cc = c as Life;
						Log.Assert(cc != null, "This should never happen!");
						return SaveLife(cc);
					}
				case "Starmaze.Game.TimedLife":
					{
						var cc = c as TimedLife;
						Log.Assert(cc != null, "This should never happen!");
						return SaveTimedLife(cc);
					}
				case "Starmaze.Game.Energy":
					{
						var cc = c as Energy;
						Log.Assert(cc != null, "This should never happen!");
						return SaveEnergy(cc);
					}
				case "Starmaze.Game.InputController":
					{
						var cc = c as InputController;
						Log.Assert(cc != null, "This should never happen!");
						return SaveInputController(cc);
					}
				case "Starmaze.Engine.Body":
					{
						var cc = c as Body;
						Log.Assert(cc != null, "This should never happen!");
						return SaveBody(cc);
					}

				case "Starmaze.Engine.RenderState":
					{
						var rend = c as RenderState;
						Log.Assert(rend != null, "This should never happen!");
						return SaveRenderState(rend);
					}
				case "Starmaze.Engine.ModelRenderState":
					{
						var rend = c as ModelRenderState;
						Log.Assert(rend != null, "This should never happen!");
						return SaveModelRenderState(rend);
					}
				case "Starmaze.Engine.BillboardRenderState":
					{
						var rend = c as BillboardRenderState;
						Log.Assert(rend != null, "This should never happen!");
						return SaveBillboardRenderState(rend);
					}
				case "Starmaze.Engine.SpriteRenderState":
					{
						var rend = c as SpriteRenderState;
						Log.Assert(rend != null, "This should never happen!");
						return SaveSpriteRenderState(rend);
					}
				default:
					var msg = String.Format("Don't know how to save component type: {0}", typ);
					throw new JsonSerializationException(msg);
			}
		}

		static Body LoadBody(Actor act, JObject json)
		{
			var gravitating = json["gravitating"].Value<bool>();
			var immobile = json["immobile"].Value<bool>();
			//var facing = 0;
			//var rotation = 0;
			//var position = 0;
			//var velocity = 0;
			//var geometry = 0;
			var b = new Body(act, gravitating, immobile);
			return b;
		}

		static BBox LoadBBox(JObject json)
		{
			return null;
		}

		static JObject SaveBody(Body b)
		{
			Log.Assert(b != null, "Shouldn't be possible!");
			var json = new JObject {
				{ "type", b.GetType().ToString() },
				{ "gravitating", b.IsGravitating },
				{ "immobile", b.IsImmobile },
			};
			return json;
		}

		static JObject SaveBBox(BBox b)
		{
			Log.Assert(b != null, "Shouldn't be possible!");
			var json = new JObject {
				{ "type", b.GetType().ToString() },
				{ "top", b.Top },
				{ "bottom", b.Bottom },
				{ "left", b.Left },
				{ "right", b.Right },
			};
			return json;
		}

		static Life LoadLife(Actor act, JObject json)
		{
			var hpIn = json["hp"].Value<double>();
			var maxLifeIn = json["maxLife"].Value<double>();
			var attenuation = json["damageAttenuation"].Value<double>();
			var damageReduction = json["damageReduction"].Value<double>();
			var l = new Life(act, hpIn, maxLifeIn, attenuation, damageReduction);
			return l;
		}

		static JObject SaveLife(Life l)
		{
			Log.Assert(l != null, "Shouldn't be possible!");
			var json = new JObject {
				{ "type", l.GetType().ToString() },
				{ "hp", l.CurrentLife },
				{ "maxLife", l.MaxLife },
				{ "damageAttenuation", l.DamageAttenuation },
				{ "damageReduction", l.DamageReduction },
			};
			return json;
		}

		static TimedLife LoadTimedLife(Actor act, JObject json)
		{
			var maxTime = json["maxTime"].Value<double>();
			var l = new TimedLife(act, maxTime);
			return l;
		}

		static JObject SaveTimedLife(TimedLife l)
		{
			Log.Assert(l != null);
			var json = new JObject {
				{ "type", l.GetType().ToString() },
				{ "maxTime", l.MaxTime },
			};
			return json;
		}

		static Energy LoadEnergy(Actor act, JObject json)
		{
			var maxEnergy = json["maxEnergy"].Value<double>();
			var regenRate = json["regenRate"].Value<double>();
			var e = new Energy(act, maxEnergy, regenRate);
			return e;
		}

		static JObject SaveEnergy(Energy e)
		{
			Log.Assert(e != null);
			var json = new JObject {
				{ "type", e.GetType().ToString() },
				{ "maxEnergy", e.MaxEnergy },
				{ "regenRate", e.RegenRate },
			};
			return json;
		}

		static InputController LoadInputController(Actor act, JObject json)
		{
			var c = new InputController(act);
			return c;
		}

		static JObject SaveInputController(InputController c)
		{
			Log.Assert(c != null);
			var json = new JObject {
				{ "type", c.GetType().ToString() },
			};
			return json;
		}

		public static T Foo<T>()
		{
			Log.Message("Foo! {0}", typeof(T));
			return default(T);
		}

		public static RenderState LoadRenderState(Actor act, JObject json)
		{
			var renderer = json["renderer"].Value<string>();
			return new RenderState(act, renderer);
		}

		public static JObject SaveRenderState(RenderState r)
		{
			Log.Assert(r != null);
			var json = new JObject {
				{ "type", r.GetType().ToString() },
				{ "renderer", r.Renderer.GetType().Name },
			};
			return json;
		}

		public static ModelRenderState LoadModelRenderState(Actor act, JObject json)
		{
			var model = Resources.TheResources.GetModel(json["model"].Value<string>());
			// Should this take a model identifier rather than the vertexarray itself, hmmm?
			return new ModelRenderState(act, model);
		}

		public static JObject SaveModelRenderState(RenderState r)
		{
			Log.Assert(r != null);
			var json = new JObject {
				{ "type", r.GetType().ToString() },
				{ "renderer", r.Renderer.GetType().Name },
				// XXX: Ow, how do we turn a random VertexArray into a string?!
			};
			return json;
		}

		public static BillboardRenderState LoadBillboardRenderState(Actor act, JObject json)
		{
			var scaleX = json["scaleX"].Value<float>();
			var scaleY = json["scaleY"].Value<float>();
			var rotation = json["rotation"].Value<float>();
			var texture = Resources.TheResources.GetTexture(json["texture"].Value<string>());
			return new BillboardRenderState(act, texture, rotation, new Vector2(scaleX, scaleY));
		}

		public static JObject SaveBillboardRenderState(RenderState r)
		{
			Log.Assert(r != null);
			var json = new JObject {
				{ "type", r.GetType().ToString() },
				{ "renderer", r.Renderer.GetType().Name },
				// XXX: Ow, how do we turn a random Texture into a string?!  It could be dynamic...
			};
			return json;
		}

		public static SpriteRenderState LoadSpriteRenderState(Actor act, JObject json)
		{
			var animation = LoadAnimations(json["animations"].Value<JArray>());
			var textureatlas = LoadTextureAtlas(json["textureAtlas"].Value<JObject>());
			var rotation = json["rotation"].Value<float>();
			var scaleX = json["scaleX"].Value<float>();
			var scaleY = json["scaleY"].Value<float>();
			return new SpriteRenderState(act, textureatlas, animation, rotation, new Vector2?(new Vector2(scaleX, scaleY)));
		}

		public static List<Animation> LoadAnimations(JArray json)
		{
			var anims = new List<Animation>();
			foreach (var val in json) {
				anims.Add(LoadAnimation(val.Value<JObject>()));
			}
			return anims;
		}

		public static Animation LoadAnimation(JObject json)
		{
			var delays = new List<double>();
			foreach (var val in json["delays"].Value<JArray>()) {
				delays.Add(val.Value<double>());
			}
			return new Animation(delays.ToArray());
		}

		public static TextureAtlas LoadTextureAtlas(JObject json)
		{
			var textureName = json["texture"].Value<string>();
			var width = json["width"].Value<int>();
			var height = json["height"].Value<int>();
			var tex = Resources.TheResources.GetTexture(textureName);
			var ta = new TextureAtlas(tex, width, height);
			return ta;
		}

		public static JObject SaveSpriteRenderState(SpriteRenderState r)
		{
			Log.Assert(r != null);
			var json = new JObject {
				{ "type", r.GetType().ToString() },
				{ "renderer", r.Renderer.GetType().Name },
				{ "animations", SaveAnimations(r.Animations) },
				{ "textureAtlas", SaveTextureAtlas(r.Atlas) },
				{ "rotation", r.Rotation },
				{ "scaleX", r.Scale.X },
				{ "scaleY", r.Scale.Y },
			};
			return json;
		}

		public static JArray SaveAnimations(IEnumerable<Animation> a)
		{
			var j = new JArray();
			foreach (var anim in a) {
				j.Add(SaveAnimation(anim));
			}
			return j;
		}

		public static JObject SaveAnimation(Animation a)
		{
			Log.Assert(a != null);
			var arr = new JArray(a.Delays);
			var json = new JObject {
				{ "type", a.GetType().ToString() },
				{ "delays", arr },
			};
			return json;
		}

		public static JObject SaveTextureAtlas(TextureAtlas t)
		{

			Log.Assert(t != null);
			var json = new JObject {
				{ "type", t.GetType().ToString() },
				{ "texture", "PlayerAssetAnimationTestSpriteSheetv3" },
				{ "width", t.Width },
				{ "height", t.Height },
			};
			return json;
		}
	}

	[TestFixture]
	public class SerializationTests
	{
		GameWindow g;
		JsonSerializerSettings jset;

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


			jset = new JsonSerializerSettings { 
				TypeNameHandling =  TypeNameHandling.Auto,
				Converters =  { new LifeConverter()} 
			};
		}

		[Test]
		public void TestJsonNetSaveLoad()
		{
			var dummy = new Actor();
			var a = new Life(dummy, 10, 15, 3, 5);
			var json = JsonConvert.SerializeObject(a, jset);
			Log.Message("Saved Life: {0}", json);
			// See, the problem here is, if we don't know the type of the object
			// then it won't look at the object's bloody type field and try to find out!
			var z1 = JsonConvert.DeserializeObject(json, typeof(Life), jset);
			Log.Message("Type of result: {0}", z1.GetType());
			Log.Message("Loaded Life: {0}", z1);
			var z2 = JsonConvert.DeserializeObject<Life>(json, jset);
			Log.Message("Type of result: {0}", z2.GetType());
			Log.Message("Loaded Life: {0}", z2);

			Assert.True(true);
		}

		[Test]
		public void TestDraconicSaveLoad()
		{
			/*
			SaveLoadThing sl = new SaveLoadThing();
			var dummy = new Actor();
			var a = new Life(dummy, 20, 30, 0.8, 2);
			var json = sl.Save(a);
			Log.Message("Saved life: {0}", json);
			var z = sl.Load(json, dummy);
			Log.Message("Loaded life: {0}", z);
Saving field Double MaxLife, value 30
			Assert.True(true);
			*/

			var ls = new LifeSaver();
			var dummy = new Actor();
			var a = new Life(dummy, 20, 30, 0.8, 2);
			var json = ls.Save(a);
			Log.Message("Saved life: {0}", json);
			var z = ls.Load(json, dummy);
			Log.Message("Loaded life: {0}", z);
			Assert.True(true);

		}

		[Test]
		public void LifeSerialTest()
		{
			var dummy = new Actor();
			var a = new Life(dummy, 20, 30, 0.8, 2);
			var json = SaveLoad.SaveComponent(a);
			Log.Message("Serialized Life: {0}", json);
			var z = SaveLoad.LoadComponent(dummy, json);
			Log.Message("Type of result: {0}", z.GetType());
			Log.Message("Result: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void TimedLifeSerialTest()
		{
			var dummy = new Actor();
			var a = new TimedLife(dummy, 14.3);
			var json = SaveLoad.SaveComponent(a);
			Log.Message("Serialized Life: {0}", json);
			var z = SaveLoad.LoadComponent(dummy, json);
			Log.Message("Type of result: {0}", z.GetType());
			Log.Message("Result: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void EnergySerialTest()
		{
			var dummy = new Actor();
			var a = new Energy(dummy, 91.3, 14.9);
			var json = SaveLoad.SaveComponent(a);
			Log.Message("Serialized Life: {0}", json);
			var z = SaveLoad.LoadComponent(dummy, json);
			Log.Message("Type of result: {0}", z.GetType());
			Log.Message("Result: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void InputControllerSerialTest()
		{
			var dummy = new Actor();
			var a = new InputController(dummy);
			var json = SaveLoad.SaveComponent(a);
			Log.Message("Serialized Life: {0}", json);
			var z = SaveLoad.LoadComponent(dummy, json);
			Log.Message("Type of result: {0}", z.GetType());
			Log.Message("Result: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void PlayerSerialTest()
		{
			var a = new Player();
			var json = SaveLoad.SaveActor(a);
			Log.Message("Serialized Player: {0}", json);
			var z = SaveLoad.LoadActor(json);
			Log.Message("Type of result: {0}", z.GetType());
			Log.Message("Result: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void PlayerLoadTest()
		{
			var json = Resources.TheResources.GetJson("player");
			var z = SaveLoad.LoadActor(json);
			Log.Message("Type of result: {0}", z.GetType());
			Log.Message("Result: {0}", z);
			Assert.True(true);
		}
	}
}
