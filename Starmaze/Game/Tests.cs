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


	public interface IAssetConverter
	{
		ISaveLoadable Load(JObject json);

		JObject Save(ISaveLoadable thing);
	}


	public class LifeAssetConverter : IAssetConverter
	{
		readonly string[] props = {
			"CurrentLife",
			"MaxLife",
			"DamageAttenuation",
			"DamageReduction",
		};

		public JObject Save(ISaveLoadable o)
		{
			o.PreSave();
			return SaveLoad.SaveProperties(o, props);
		}

		public ISaveLoadable Load(JObject json)
		{
			var l = new Life(null, 0);
			SaveLoad.LoadProperties(l, props, json);
			l.PostLoad();
			return l;
		}
	}

	public static class SaveLoad
	{
		static Dictionary<Type, IAssetConverter> SaveLoaders = new Dictionary<Type, IAssetConverter> {
			{ typeof(Life), new LifeAssetConverter() },
		};

		public static JObject SaveProperties(ISaveLoadable o, string[] props)
		{
			var typ = o.GetType();
			var json = new JObject();
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
					// This cast is a little screwy and unpleasant, and I'm not sure it's the right thing.
					var isl = val as ISaveLoadable;
					Log.Assert(isl != null, "This should never happen???");
					json[propName] = Save(isl);
				} else {
					var msg = String.Format("Can't save object of type {0} of property {1} on object {2}", propType, propName, typ);
					throw new JsonSerializationException(msg);
				}
			}
			json.Add("type", typ.ToString());
			return json;
		}

		public static void LoadProperties(ISaveLoadable o, string[] props, JObject json)
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

		public static JObject Save(ISaveLoadable o)
		{
			var typ = o.GetType();
			if (IsJValue(typ)) {
				return new JObject(o);
			} else {
				return DispatchSave(o, typ);
			}
		}

		public static ISaveLoadable Load(JObject json)
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


		static JObject DispatchSave(ISaveLoadable o, Type typ)
		{
			IAssetConverter saveLoader;
			if (SaveLoaders.TryGetValue(typ, out saveLoader)) {
				return saveLoader.Save(o);
			} else {
				var msg = String.Format("Could not find function to save type {0}", typ);
				throw new JsonSerializationException(msg);
			}
		}

		static ISaveLoadable DispatchLoad(JObject o, Type typ)
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

	/*


	public class GenericSaver : ISaveLoad
	{

		public string[] Props { get; set; }

		public Type TargetType;

		public GenericSaver(Type target)
		{
			TargetType = target;
			Props = new string[] {

			};
		}

		public ISaveLoadable Load(JObject json, object accessory)
		{
			var act = accessory as Actor;
			Log.Assert(act != null, "Aiee!");
			var l = TargetType.TypeInitializer.Invoke(new object[] { act });
			var isl = l as ISaveLoadable;
			//var l = new Life(act, 0);
			var typ = l.GetType();
			foreach (var propName in Props) {
				var property = typ.GetProperty(propName);
				Log.Assert(property != null, "Property {0} not found on object of type {1}!", propName, typ);
				var loadedValue = json[propName].ToObject(property.PropertyType);
				Log.Message("Setting property {0} to {1}", property, loadedValue);
				property.SetValue(l, loadedValue);
			}
			isl.PostLoad();
			return isl;
		}

		public JObject Save(ISaveLoadable l)
		{
			Log.Assert(l != null, "Shouldn't be possible!");
			l.PreSave();
			var typ = l.GetType();
			var json = new JObject();
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
			}
			json.Add("type", typ.ToString());
			return json;
		}

		public bool IsJValue(Type t)
		{
			return t == typeof(long) || t == typeof(int) || t == typeof(decimal) || t == typeof(char) ||
			t == typeof(ulong) || t == typeof(float) || t == typeof(double) || t == typeof(DateTime) ||
			t == typeof(DateTimeOffset) || t == typeof(bool) || t == typeof(string) || t == typeof(Guid) ||
			t == typeof(TimeSpan) || t == typeof(Uri);
		}


		public bool IsSaveable(Type t)
		{
			return IsJValue(t) || SaveFuncs.ContainsKey(t);
		}

		public JObject SaveThing(object o)
		{
			var typ = o.GetType();
			if (IsJValue(typ)) {
				return new JObject(o);
			} else {
				return DispatchSave(o, typ);
			}
		}

		Dictionary<Type, Func<object, JValue>> SaveFuncs;

		public JObject DispatchSave(object o, Type typ)
		{
			return new JObject();
		}

		public JObject SaveProperties(object o, string[] props)
		{
			var typ = o.GetType();
			var json = new JObject();
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
					json[propName] = SaveThing(val);
				} else {
					var msg = String.Format("Can't save object of type {0} of property {1} on object {2}", propType, propName, typ);
					throw new JsonSerializationException(msg);
				}
			}
			json.Add("type", typ.ToString());
			return json;
		}

		public void LoadProperties(object o, string[] props, JObject json)
		{
			var isl = o as ISaveLoadable;
			var typ = o.GetType();
			foreach (var propName in Props) {
				var property = typ.GetProperty(propName);
				Log.Assert(property != null, "Property {0} not found on object of type {1}!", propName, typ);
				var loadedValue = json[propName].ToObject(property.PropertyType);
				Log.Message("Setting property {0} to {1}", property, loadedValue);
				property.SetValue(o, loadedValue);
			}
		}

		public JValue SaveJSONNative(object val)
		{
			return new JValue(val);
		}
	}

    public class LifeSaver : GenericSaver
    {

        public LifeSaver() : base(typeof(Life))
        {
            Props = new string[] {
                "CurrentLife",
                "MaxLife",
                "DamageAttenuation",
                "DamageReduction",
            };
        }
    }

    public class BodySaver : GenericSaver
    {
        public BodySaver() : base(typeof(Body))
        {
            Props = new string[] {
                "IsGravitating",
                "IsImmobile",
            };
        }
    }


	public class SaveLoadThing
	{
		Dictionary<Type, ISaveLoad> SLDict;

		public SaveLoadThing()
		{
			SLDict = new Dictionary<Type, ISaveLoad> {
				{ typeof(Life), new LifeSaver() },
				{ typeof(Body), new BodySaver() },
			};
		}

		public JObject Save(ISaveLoadable o)
		{
			var typ = o.GetType();
			ISaveLoad saver;
			if (!SLDict.TryGetValue(typ, out saver)) {
				var msg = String.Format("Could not find saver for type {0}", typ);
				throw new Exception(msg);
			}
			return saver.Save(o);
		}

		public T Load<T>(JObject json, object accessory)
		{
			return (T)Load(json, accessory);
		}

		public ISaveLoadable Load(JObject json, object accessory)
		{
			var typeName = json["type"].Value<string>();
			var typ = Type.GetType(typeName);
			ISaveLoad loader;
			if (!SLDict.TryGetValue(typ, out loader)) {
				var msg = String.Format("Could not find loader for type {0}", typeName);
				throw new Exception(msg);
			}
			return loader.Load(json, accessory);
		}
	}

*/

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
		public void DraconicLifeSaveLoadTest()
		{
			//var sl = new TryFour();
			var dummy = new Actor();
			var a = new Life(dummy, 20, 30, 0.8, 2);
			var json = SaveLoad.Save(a);
			Log.Message("Saved life: {0}", json);
			var z = SaveLoad.Load(json);
			Log.Message("Loaded life: {0}", z);
			Assert.True(true);
		}
		/*
		[Test]
		public void DraconicBodySaveLoadTest()
		{
			SaveLoadThing sl = new SaveLoadThing();
			var dummy = new Actor();
			var a = new Body(dummy, true, false);
			var json = sl.Save(a);
			Log.Message("Saved body: {0}", json);
			var z = sl.Load(json, dummy);
			Log.Message("Loaded body: {0}", z);
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
*/
	}
}
