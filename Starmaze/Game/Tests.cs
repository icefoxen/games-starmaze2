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
	static class ComponentConverter
	{
		public static Component LoadComponent(JObject json)
		{
			try {
				var typ = json["type"].Value<string>();
				Log.Message("Type name: {0}", typ);
				// This is a little ugly, but it gets everything hard-coded in at compile time,
				// which is exactly what we want, semantically.  Also prevents irritating type
				// shenanigans when we try to put bunches of functions into a collection or such.
				switch (typ) {
					case "Life":
						return LoadLife(json);
					default:
						return null;
				}
			} catch (KeyNotFoundException e) {
				throw e;
			}
		}

		public static JObject SaveComponent(Component c)
		{
			var typ = c.GetType();
			Log.Message("Type name: {0}", typ.Name);
			// This is a little ugly, but it gets everything hard-coded in at compile time,
			// which is exactly what we want, semantically.  Also prevents irritating type
			// shenanigans when we try to put bunches of functions into a collection or such.
			switch (typ.Name) {
				case "Life":
					return SaveLife(c as Life);
				default:
					return new JObject();
			}
		}

		public static Life LoadLife(JObject json)
		{
			var hpIn = json["hp"].Value<double>();
			var maxLifeIn = json["maxLife"].Value<double>();
			var attenuation = json["damageAttenuation"].Value<double>();
			var damageReduction = json["damageReduction"].Value<double>();
			var l = new Life(null, hpIn, maxLifeIn, attenuation, damageReduction);
			return l;
		}

		public static JObject SaveLife(Life l)
		{
			Log.Assert(l != null, "Shouldn't be possible!");
			var json = new JObject {
				{"type", l.GetType().Name},
				{"hp", l.CurrentLife},
				{"maxLife", l.MaxLife},
				{"damageAttenuation", l.DamageAttenuation},
				{"damageReduction", l.DamageReduction},
			};
			return json;
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
		}

		[Test]
		public void LifeSerialTest()
		{
			var dummy = new Actor();
			var a = new Life(dummy, 10);
			var json = ComponentConverter.SaveComponent(a);
			Log.Message("Life: {0}", json);
			//var j = Newtonsoft.Json.Linq.JObject.Parse(json);
			//var typ = Type.GetType(j["$type"].ToString());
			//Log.Message("Life type: {0}", typ);
			var z = ComponentConverter.LoadComponent(json);
			Log.Message("Type of result: {0}", z.GetType());
			Log.Message("Result: {0}", z);
			//Log.Message()
			Assert.True(true);
		}
	}
}