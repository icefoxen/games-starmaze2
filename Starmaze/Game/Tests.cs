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

	public class TryFour
	{
		Dictionary<Type, Func<object, JObject>> SaveFuncs;
		Dictionary<Type, Func<JObject, object>> LoadFuncs;

		public TryFour()
		{
			SaveFuncs = new Dictionary<Type, Func<object, JObject>> {
				{ typeof(Life), SaveLife },
			};

			LoadFuncs = new Dictionary<Type, Func<JObject, object>> {
				{ typeof(Life), LoadLife },
			};
		}

		JObject SaveLife(object o)
		{
			var props = new string[] {
				"CurrentLife",
				"MaxLife",
				"DamageAttenuation",
				"DamageReduction",
			};
			return SaveProperties(o, props);
		}

		object LoadLife(JObject json)
		{
			var props = new string[] {
				"CurrentLife",
				"MaxLife",
				"DamageAttenuation",
				"DamageReduction",
			};
			var l = new Life(null, 0);
			LoadProperties(l, props, json);
			return l;
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

		public JObject Save(object o)
		{
			var typ = o.GetType();
			if (IsJValue(typ)) {
				return new JObject(o);
			} else {
				return DispatchSave(o, typ);
			}
		}

		public object Load(JObject json)
		{
			var typeName = json["type"].Value<string>();
			Log.Assert(typeName != null);
			var typ = Type.GetType(typeName);
			Log.Assert(typ != null);
			if (IsJValue(typ)) {
				return null; // XXX: Hmmm.
			} else {
				return DispatchLoad(json, typ);
			}
		}

		public T Load<T>(JObject json)
		{
			return (T)Load(json);
		}


		JObject DispatchSave(object o, Type typ)
		{
			Func<object, JObject> saveFunc;
			if (SaveFuncs.TryGetValue(typ, out saveFunc)) {
				return saveFunc(o);
			} else {
				var msg = String.Format("Could not find function to save type {0}", typ);
				throw new JsonSerializationException(msg);
			}
		}

		object DispatchLoad(JObject o, Type typ)
		{
			Func<JObject, object> loadFunc;
			if (LoadFuncs.TryGetValue(typ, out loadFunc)) {
				return loadFunc(o);
			} else {
				var msg = String.Format("Could not find function to load type {0}", typ);
				throw new JsonSerializationException(msg);
			}
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
					json[propName] = Save(val);
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
			foreach (var propName in props) {
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

	/*
	public interface ISaveLoad
	{
		ISaveLoadable Load(JObject json, object accessory);

		JObject Save(ISaveLoadable thing);

		string[] Props { get; }
	}


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
			var z1 = JsonConvert.DeserializeObject(json, jset);
			Log.Message("Type of result: {0}", z1.GetType());
			Log.Message("Loaded Life: {0}", z1);
			var z2 = JsonConvert.DeserializeObject<Life>(json, jset);
			Log.Message("Type of result: {0}", z2.GetType());
			Log.Message("Loaded Life: {0}", z2);

			Assert.True(true);
		}

		[Test]
		public void DraconicLifeSaveLoadTest()
		{
			var sl = new TryFour();
			var dummy = new Actor();
			var a = new Life(dummy, 20, 30, 0.8, 2);
			var json = sl.Save(a);
			Log.Message("Saved life: {0}", json);
			var z = sl.Load(json);
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
