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


	public interface IAssetConverter
	{
		object Load(JObject json);

		JObject Save(object thing);
	}


	public class LifeAssetConverter : IAssetConverter
	{
		readonly string[] props = {
			"CurrentLife",
			"MaxLife",
			"DamageAttenuation",
			"DamageReduction",
		};

		public JObject Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			return SaveLoad.SaveProperties(o, props);
		}

		public object Load(JObject json)
		{
			var obj = new Life(null, 0);
			SaveLoad.LoadProperties(obj, props, json);
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

	public class EnergyAssetConverter : IAssetConverter
	{
		readonly string[] props = {
			"MaxEnergy",
			"RegenRate",
		};

		public JObject Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			return SaveLoad.SaveProperties(o, props);
		}

		public object Load(JObject json)
		{
			var obj = new Energy(null);
			SaveLoad.LoadProperties(obj, props, json);
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

	public class BodyAssetConverter : IAssetConverter
	{
		readonly string[] props = {
			"Position",
			"Velocity",
			// BUGGO: This needs to be handled specially 'cause it's an enum.
			//"Facing",
			"Rotation",
			"Mass",
			"IsGravitating",
			"IsImmobile",
		};

		public JObject Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			return SaveLoad.SaveProperties(o, props);
		}

		public object Load(JObject json)
		{
			var obj = new Body(null);
			SaveLoad.LoadProperties(obj, props, json);
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

	public class Vector2dAssetConverter : IAssetConverter
	{
		// Vector2d X and Y aren't properties, so things get a little weird.
		public JObject Save(object o)
		{
			var obj = (Vector2d)o;
			Log.Assert(obj != null);
			var json = SaveLoad.JObjectOfType(o);
			json["X"] = obj.X;
			json["Y"] = obj.Y;
			return json;
		}

		public object Load(JObject json)
		{
			var obj = new Vector2d();
			obj.X = json["X"].Value<double>();
			obj.Y = json["Y"].Value<double>();
			return obj;
		}
	}

	public class InputControllerAssetConverter : IAssetConverter
	{
		public JObject Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			return SaveLoad.JObjectOfType(o);
		}

		public object Load(JObject json)
		{
			var obj = new InputController(null);
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

	public class ActorAssetConverter : IAssetConverter
	{
		public JObject Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			var act = o as Actor;
			Log.Assert(act != null, "Shouldn't be possible");
			var json = SaveLoad.JObjectOfType(o);
			json["Components"] = SaveLoad.SaveList<Component>(act.Components);
			return json;
		}

		public object Load(JObject json)
		{
			var obj = new Actor();
			var components = SaveLoad.LoadList<Component>(json["Components"].Value<JArray>());
			foreach (var c in components) {
				obj.AddComponent(c);
			}
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

	public static class SaveLoad
	{
		static Dictionary<Type, IAssetConverter> SaveLoaders = new Dictionary<Type, IAssetConverter> {
			{ typeof(Life), new LifeAssetConverter() },
			{ typeof(Energy), new EnergyAssetConverter() },
			{ typeof(Body), new BodyAssetConverter() },
			{ typeof(Vector2d), new Vector2dAssetConverter() },
			{ typeof(InputController), new InputControllerAssetConverter() },
			{ typeof(Actor), new ActorAssetConverter() },
		};

		public static JObject SaveProperties(object o, string[] props)
		{
			var typ = o.GetType();
			var json = JObjectOfType(o);
			foreach (var propName in props) {
				//var field = typ.GetField(propName);
				//var val = field.GetValue(l);
				var property = typ.GetProperty(propName);
				var val = property.GetValue(o);
				Log.Message("Saving field {0}, value {1}", property, val);
				var propType = property.PropertyType;
				if (IsJValue(propType)) {
					json[propName] = new JValue(val);
				} else if (IsSaveable(propType)) {
					json[propName] = Save(val);
				} else {
					var msg = String.Format("Can't save object of type {0} of property {1} on object {2}", propType, propName, typ);
					throw new JsonSerializationException(msg);
				}
			}
			return json;
		}

		public static JArray SaveList<T>(IEnumerable<T> lst)
		{
			var json = new JArray();
			foreach (var item in lst) {
				json.Add(Save(item));
			}
			return json;
		}

		public static IEnumerable<T> LoadList<T>(JArray json)
		{
			var list = new List<T>();
			foreach (var item in json) {
				Log.Message("Item: {0}", item);
				var jobj = item as JObject;
				Log.Assert(jobj != null, "Should never happen...");
				var c = Load<T>(jobj);
				list.Add(c);
			}
			return list;
		}

		public static void PostLoadIfPossible(object o)
		{
			var typ = o.GetType();
			var postLoadMethod = typ.GetMethod("PostLoad");
			if (postLoadMethod != null) {
				postLoadMethod.Invoke(o, null);
			}
		}

		public static void PreSaveIfPossible(object o)
		{
			var typ = o.GetType();
			var preSaveMethod = typ.GetMethod("PreSave");
			if (preSaveMethod != null) {
				preSaveMethod.Invoke(o, null);
			}
		}

		public static void LoadProperties(object o, string[] props, JObject json)
		{
			var typ = o.GetType();
			foreach (var propName in props) {
				var property = typ.GetProperty(propName);
				Log.Assert(property != null, "Property {0} not found on object of type {1}!", propName, typ);
				var loadedValue = json[propName].ToObject(property.PropertyType);
				Log.Message("Setting property {0} to {1}", property, loadedValue);
				property.SetValue(o, loadedValue);
			}
		}



		static bool IsJValue(Type t)
		{
			return t == typeof(long) || t == typeof(int) || t == typeof(decimal) || t == typeof(char) ||
			t == typeof(ulong) || t == typeof(float) || t == typeof(double) || t == typeof(DateTime) ||
			t == typeof(DateTimeOffset) || t == typeof(bool) || t == typeof(string) || t == typeof(Guid) ||
			t == typeof(TimeSpan) || t == typeof(Uri);
		}


		static bool IsSaveable(Type t)
		{
			return IsJValue(t) || SaveLoaders.ContainsKey(t);
		}

		public static JObject Save(object o)
		{
			var typ = o.GetType();
			if (IsJValue(typ)) {
				return new JObject(o);
			} else {
				return DispatchSave(o, typ);
			}
		}

		public static object Load(JObject json)
		{
			var typeName = json["type"].Value<string>();
			Log.Assert(typeName != null);
			var typ = Type.GetType(typeName);
			Log.Assert(typ != null);
			return DispatchLoad(json, typ);
//			if (IsJValue(typ)) {
//				return null; // XXX: Hmmm.
//			} else {
//				return DispatchLoad(json, typ);
//			}
		}

		public static T Load<T>(JObject json)
		{
			return (T)Load(json);
		}

		public static JObject JObjectOfType(object o)
		{
			var t = o.GetType();
			var json = new JObject {
				{ "type", t.ToString() },
			};
			return json;
		}


		static JObject DispatchSave(object o, Type typ)
		{
			IAssetConverter saveLoader;
			if (SaveLoaders.TryGetValue(typ, out saveLoader)) {
				return saveLoader.Save(o);
			} else {
				var msg = String.Format("Could not find function to save type {0}", typ);
				throw new JsonSerializationException(msg);
			}
		}

		static object DispatchLoad(JObject o, Type typ)
		{
			IAssetConverter saveLoader;
			if (SaveLoaders.TryGetValue(typ, out saveLoader)) {
				return saveLoader.Load(o);
			} else {
				var msg = String.Format("Could not find function to load type {0}", typ);
				throw new JsonSerializationException(msg);
			}
		}

		public static JValue SaveJSONNative(object val)
		{
			return new JValue(val);
		}
	}



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
			var json = SaveLoad.Save(a);
			Log.Message("Saved non-empty actor: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded non-empty actor: {0}", z);
			Assert.True(true);
		}
	}
}
