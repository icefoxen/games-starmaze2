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
	interface ISaveLoad
	{
		JObject SaveTo();

		void LoadFrom(JObject j);
	}

	class LifeConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return (objectType == typeof(Life));
		}

		public override bool CanWrite {
			get{ return true; }
		}

		public override bool CanRead {
			get{ return true; }
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var l = value as Life;
			Log.Assert(l != null);
			var json = new JObject {
				{"type", l.GetType().ToString()},
				{"hp", l.CurrentLife},
				{"maxLife", l.MaxLife},
				{"damageAttenuation", l.DamageAttenuation},
				{"damageReduction", l.DamageReduction},
			};
			json.WriteTo(writer);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			Log.Assert(objectType == typeof(Life));
			var json = JObject.Load(reader);
			var hpIn = json["hp"].Value<double>();
			var maxLifeIn = json["maxLife"].Value<double>();
			var attenuation = json["damageAttenuation"].Value<double>();
			var damageReduction = json["damageReduction"].Value<double>();
			var l = new Life(null, hpIn, maxLifeIn, attenuation, damageReduction);
			return l;
		}
	}

	class TimedLifeConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return (objectType == typeof(TimedLife));
		}

		public override bool CanWrite {
			get{ return true; }
		}

		public override bool CanRead {
			get{ return true; }
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var l = value as TimedLife;
			Log.Assert(l != null);
			var json = new JObject {
				{"type", l.GetType().ToString()},
				{"maxTime", l.MaxTime},
			};
			json.WriteTo(writer);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			Log.Assert(objectType == typeof(TimedLife));
			var json = JObject.Load(reader);
			var maxTime = json["maxTime"].Value<double>();
			var l = new TimedLife(null, maxTime);
			return l;
		}
	}

	class EnergyConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return (objectType == typeof(Energy));
		}

		public override bool CanWrite {
			get{ return true; }
		}

		public override bool CanRead {
			get{ return true; }
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var e = value as Energy;
			Log.Assert(e != null);
			var json = new JObject {
				{"type", e.GetType().ToString()},
				{"maxEnergy", e.MaxEnergy},
				{"regenRate", e.RegenRate},
			};
			json.WriteTo(writer);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			Log.Assert(objectType == typeof(Energy));
			var json = JObject.Load(reader);
			var maxEnergy = json["maxEnergy"].Value<double>();
			var regenRate = json["regenRate"].Value<double>();
			var e = new Energy(null, maxEnergy, regenRate);
			return e;
		}
	}

	class InputControllerConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return (objectType == typeof(InputController));
		}

		public override bool CanWrite {
			get{ return true; }
		}

		public override bool CanRead {
			get{ return true; }
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var e = value as InputController;
			Log.Assert(e != null);
			var json = new JObject {
				{"type", e.GetType().ToString()},
			};
			json.WriteTo(writer);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			Log.Assert(objectType == typeof(InputController));
			var json = JObject.Load(reader);
			var e = new InputController(null);
			return e;
		}
	}

	class ComponentConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return (objectType == typeof(Component));
		}

		public override bool CanWrite {
			get{ return true; }
		}

		public override bool CanRead {
			get{ return true; }
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var g = value as Component;
			Log.Assert(g != null);
			var json = new JObject {
				{"type", g.GetType().ToString()},
			};
			json.WriteTo(writer);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			Log.Assert(objectType == typeof(Component));
			var json = JObject.Load(reader);
			var t = Type.GetType(json["type"].Value<string>());

			var g = new Gun(null);
			return g;
		}
	}

	class GunConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return (objectType == typeof(Gun));
		}

		public override bool CanWrite {
			get{ return true; }
		}

		public override bool CanRead {
			get{ return true; }
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var g = value as Gun;
			Log.Assert(g != null);
			var json = new JObject {
				{"type", g.GetType().ToString()},
			};
			json.WriteTo(writer);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			Log.Assert(objectType == typeof(Gun));
			var json = JObject.Load(reader);
			var g = new Gun(null);
			return g;
		}
	}

	[TestFixture]
	public class SerializationTests
	{
		JsonSerializerSettings jset;
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
			jset = new JsonSerializerSettings { };//TypeNameHandling = TypeNameHandling.All };
			jset.Converters.Add(new LifeConverter());
			jset.Converters.Add(new TimedLifeConverter());
			jset.Converters.Add(new EnergyConverter());
			jset.Converters.Add(new InputControllerConverter());
		}

		[Test]
		public void LifeSerialTest()
		{
			var a = new Life(null, 20, 30, 0.8, 2);
			var json = JsonConvert.SerializeObject(a, jset);
			Log.Message("Serialized Life: {0}", json);
			// This is sorta messy 'cause we have to parse the json twice...
			// Not necessary for this example, but it will be for reals when we are
			// trying to find out what kind of component we're creating.
			var jo = JObject.Parse(json);
			var typ = Type.GetType(jo["type"].Value<string>());
			//Log.Message("Should be type '{0}'", typ);
			var z = JsonConvert.DeserializeObject(json, typ, jset);
			//Log.Message("Type of result: {0}", z.GetType());
			Log.Message("Result: {0}", z);
			//Log.Message()
			Assert.True(true);
		}

		[Test]
		public void TimedLifeSerialTest()
		{
			var a = new TimedLife(null, 14.3);
			var json = JsonConvert.SerializeObject(a, jset);
			Log.Message("Serialized TimedLife: {0}", json);
			var jo = JObject.Parse(json);
			var typ = Type.GetType(jo["type"].Value<string>());
			var z = JsonConvert.DeserializeObject(json, typ, jset);
			Log.Message("Type of result: {0}", z.GetType());
			Log.Message("Result: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void EnergySerialTest()
		{
			var a = new Energy(null, 50, 3);
			var json = JsonConvert.SerializeObject(a, jset);
			Log.Message("Serialized Energy: {0}", json);
			var jo = JObject.Parse(json);
			var typ = Type.GetType(jo["type"].Value<string>());
			var z = JsonConvert.DeserializeObject(json, typ, jset);
			Log.Message("Type of result: {0}", z.GetType());
			Log.Message("Result: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void InputControllerTest()
		{
			var a = new InputController(null);
			var json = JsonConvert.SerializeObject(a, jset);
			Log.Message("Serialized InputController: {0}", json);
			var jo = JObject.Parse(json);
			var typ = Type.GetType(jo["type"].Value<string>());
			var z = JsonConvert.DeserializeObject(json, typ, jset);
			Log.Message("Type of result: {0}", z.GetType());
			Log.Message("Result: {0}", z);
			Assert.True(true);
		}

		[Test]
		public void ComponentSerializeTest()
		{
			
			var a = new Life(null, 20, 30, 0.8, 2);
			var b = new TimedLife(null, 14.3);
			var c = new Energy(null, 50, 3);
			var d = new InputController(null);

			var l = new Component[] {
				a, b, c, d,
			};

			var json = JsonConvert.SerializeObject(l, jset);
			Log.Message("Serialized components: {0}", json);
			
			var jo = JObject.Parse(json);

			foreach(var item in jo.Children()) {
				var typ = Type.GetType(item["type"].Value<string>());
				var z = JsonConvert.DeserializeObject(item.CreateReader().ToString(), typ, jset);
				Log.Message("Type of result: {0}", z.GetType());
				Log.Message("Result: {0}", z);

			}

			Assert.True(true);
		}
	}
}