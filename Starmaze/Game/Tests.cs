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
			jset = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
			jset.Converters.Add(new LifeConverter());
		}

		[Test]
		public void LifeSerialTest()
		{
			var dummy = new Actor();
			var a = new Life(dummy, 20, 30, 0.8, 2);
			var json = JsonConvert.SerializeObject(a, jset);
			var jj = JsonSerializer.Create(jset);
			Log.Message("Serialized Life: {0}", json);
			// This is sorta messy 'cause we have to parse the json twice...
			// Not necessary for this example, but it will be for reals when we are
			// trying to find out what kind of component we're creating.
			var jo = JObject.Parse(json);
			var typ = Type.GetType(jo["type"].Value<string>());
			Log.Message("Should be type '{0}'", typ);
			var z = JsonConvert.DeserializeObject(json, typ, jset);
			Log.Message("Type of result: {0}", z.GetType());
			Log.Message("Result: {0}", z);
			//Log.Message()
			Assert.True(true);
		}
	}
}