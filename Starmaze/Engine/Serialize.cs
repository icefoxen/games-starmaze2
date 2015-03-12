using System;
using System.Collections.Generic;
using OpenTK;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Starmaze.Engine
{
	class ActorConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return (objectType == typeof(Actor));
		}

		public override bool CanWrite {
			get{ return true; }
		}

		public override bool CanRead {
			get{ return true; }
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var a = value as Actor;
			Log.Assert(a != null);
			var json = new JObject {
				{"type", a.GetType().ToString()},
				//{"components", a.Components},
				//{"renderState", a.RenderState},
				{"keepOnRoomChange", a.KeepOnRoomChange},
			};
			json.WriteTo(writer);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			Log.Assert(objectType == typeof(Actor));
			var json = JObject.Load(reader);
			var components = json["components"].Value<IEnumerable<Component>>();
			var keepOnRoomChange = json["keepOnRoomChange"].Value<bool>();
			var renderState = json["renderState"].Value<RenderState>();
			var a = new Actor();
			a.RenderState = renderState;
			a.KeepOnRoomChange = keepOnRoomChange;
			foreach (var c in components) {
				a.AddComponent(c);
			}
			return a;
		}
	}

	/// <summary>
	/// OTK vector2d converter. Default serialization behavior gets circular references
	/// Probably due to vectors having a useful diverse way to be specified
	/// Serialization is based on only the X and Y values.
	/// </summary>
	class OTKVector2dConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return (objectType == typeof(OpenTK.Vector2d));
		}

		public override bool CanRead {
			get{ return true; }
		}

		public override bool CanWrite {
			get{ return true; }
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			//HashSet values =  serializer.Deserialize<HashSet> (reader);

			//Console.WriteLine ("Custom converter used");
			return serializer.Deserialize<Vector2d>(reader);

		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			Vector2d vector = (Vector2d)value;
			writer.WriteStartObject();
			writer.WritePropertyName("X");
			writer.WriteValue(vector.X);
			writer.WritePropertyName("Y");
			writer.WriteValue(vector.Y);
			writer.WriteEndObject();
		}
	}
}
