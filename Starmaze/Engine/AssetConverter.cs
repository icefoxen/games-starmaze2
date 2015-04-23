using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTK;

namespace Starmaze.Engine
{
	public interface IAssetConverter
	{
		object Load(JToken json);

		JToken Save(object thing);
	}



	public class Vector2dAssetConverter : IAssetConverter
	{
		// Vector2d X and Y aren't properties, so things get a little weird.
		public JToken Save(object o)
		{
			var obj = (Vector2d)o;
			var json = SaveLoad.JObjectOfType(o);
			json["X"] = obj.X;
			json["Y"] = obj.Y;
			return json;
		}

		public object Load(JToken json)
		{
			var obj = new Vector2d();
			obj.X = json["X"].Value<double>();
			obj.Y = json["Y"].Value<double>();
			return obj;
		}
	}

	public class Vector2AssetConverter : IAssetConverter
	{
		// Vector2d X and Y aren't properties, so things get a little weird.
		public JToken Save(object o)
		{
			var obj = (Vector2)o;
			var json = SaveLoad.JObjectOfType(o);
			json["X"] = obj.X;
			json["Y"] = obj.Y;
			return json;
		}

		public object Load(JToken json)
		{
			var obj = new Vector2();
			obj.X = json["X"].Value<float>();
			obj.Y = json["Y"].Value<float>();
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

		public JToken Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			return SaveLoad.SaveProperties(o, props);
		}

		public object Load(JToken json)
		{
			var obj = new Body();
			SaveLoad.LoadProperties(obj, props, json);
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}



	public class ActorAssetConverter : IAssetConverter
	{
		public JToken Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			var act = o as Actor;
			Log.Assert(act != null, "Shouldn't be possible");
			var json = SaveLoad.JObjectOfType(o);
			json["Components"] = SaveLoad.SaveList<Component>(act.Components);
			return json;
		}

		public object Load(JToken json)
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

	public class TextureAtlasAssetConverter : IAssetConverter
	{
		readonly string[] props = {
			"Width",
			"Height",
		};

		public JToken Save(object o)
		{
			// BUGGO: Need some way to turn an arbitrary texture into a texture name.
			// The problem then is dynamic textures!
			//Log.Assert(false, "Saving a texture can't really be done right now I fear...");
			SaveLoad.PreSaveIfPossible(o);
			var json = SaveLoad.SaveProperties(o, props);
			json["Texture"] = "playertest";
			return json;
		}

		public object Load(JToken json)
		{
			var texName = json["Texture"].Value<string>();
			var tex = Resources.TheResources.GetTexture(texName);
			var obj = new TextureAtlas(tex, 1, 1);
			SaveLoad.LoadProperties(obj, props, json);
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

	public class AnimationAssetConverter : IAssetConverter
	{

		public JToken Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			var anim = o as Animation;
			Log.Assert(anim != null, "Shouldn't be possible");
			var json = SaveLoad.JObjectOfType(o);
			json["Delays"] = SaveLoad.SaveList<double>(anim.Delays);
			return json;
		}

		public object Load(JToken json)
		{
			var framesArray = json["Delays"].Value<JArray>();
			Log.Message("Frames: {0}", framesArray);
			var delays = SaveLoad.LoadList<double>(framesArray);
			var arr = new List<double>(delays).ToArray();
			var obj = new Animation(arr);
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

	public class SpriteRenderStateAssetConverter : IAssetConverter
	{
		readonly string[] props = {
			"Rotation",
			"Scale",
		};

		public JToken Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			var sprite = o as SpriteRenderState;
			Log.Assert(sprite != null, "Should be impossible");
			var json = SaveLoad.SaveProperties(o, props);
			json["TextureAtlas"] = SaveLoad.Save(sprite.Atlas);
			json["Animations"] = SaveLoad.SaveList<Animation>(sprite.Animations);
			return json;
		}

		public object Load(JToken json)
		{
			var atlas = SaveLoad.Load(json["TextureAtlas"].Value<JObject>()) as TextureAtlas;
			var anim = SaveLoad.LoadList<Animation>(json["Animations"].Value<JArray>());
			Log.Assert(anim != null);
			var obj = new SpriteRenderState(atlas, anim);
			SaveLoad.LoadProperties(obj, props, json);
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

	public class RoomAssetConverter : IAssetConverter
	{
		readonly string[] props = {
			"Name",
		};

		public JToken Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			var room = o as Room;
			Log.Assert(room != null, "Should be impossible");
			var json = SaveLoad.SaveProperties(o, props);
			json["Actors"] = SaveLoad.SaveList<Actor>(room.Actors);
			return json;
		}

		public object Load(JToken json)
		{
			var actors = SaveLoad.LoadList<Actor>(json["Actors"].Value<JArray>());
			Log.Assert(actors != null);
			var obj = new Room("", actors);
			SaveLoad.LoadProperties(obj, props, json);
			obj.Actors = actors;
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

	public class ZoneAssetConverter : IAssetConverter
	{
		readonly string[] props = {
			"Name",
		};

		public JToken Save(object o)
		{
			var zone = o as Zone;
			Log.Assert(zone != null, "Should be impossible");
			SaveLoad.PreSaveIfPossible(o);
			var json = SaveLoad.SaveProperties(o, props);
			json["Rooms"] = SaveLoad.SaveList<Room>(zone.GetRooms());
			return json;
		}

		public object Load(JToken json)
		{
			var rooms = SaveLoad.LoadList<Room>(json["Rooms"].Value<JArray>());
			Log.Assert(rooms != null);
			var obj = new Zone("", rooms);
			SaveLoad.LoadProperties(obj, props, json);
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

	public static class SaveLoad
	{
		// XXX: Dependency inversion here, try to fix someday.
		static readonly Dictionary<Type, IAssetConverter> AssetConverters = new Dictionary<Type, IAssetConverter> {
			{ typeof(Vector2), new Vector2AssetConverter() },
			{ typeof(Vector2d), new Vector2dAssetConverter() },
			{ typeof(Body), new BodyAssetConverter() },
			{ typeof(Actor), new ActorAssetConverter() },
			{ typeof(SpriteRenderState), new SpriteRenderStateAssetConverter() },
			{ typeof(Animation), new AnimationAssetConverter() },
			{ typeof(TextureAtlas), new TextureAtlasAssetConverter() },
			{ typeof(Room), new RoomAssetConverter() },
			{ typeof(Zone), new ZoneAssetConverter() },
			{ typeof(Starmaze.Game.Life), new Starmaze.Game.LifeAssetConverter() },
			{ typeof(Starmaze.Game.Energy), new Starmaze.Game.EnergyAssetConverter() },
			{ typeof(Starmaze.Game.InputController), new Starmaze.Game.InputControllerAssetConverter() },
			{ typeof(Starmaze.Game.TimedLife), new Starmaze.Game.TimedLifeAssetConverter() },
			{ typeof(Starmaze.Game.Gun), new Starmaze.Game.GunAssetConverter() },
			{ typeof(Starmaze.Game.PowerSet), new Starmaze.Game.PowerSetAssetConverter() },
		};

		public static void AddConverter(Type type, IAssetConverter converter)
		{
			AssetConverters.Add(type, converter);
		}

		// Might not work the way we want, but, oh well.
		public static void AddConverters(IEnumerable<KeyValuePair<Type, IAssetConverter>> items)
		{
			foreach (var kv in items) {
				AssetConverters.Add(kv.Key, kv.Value);
			}
		}

		public static JObject SaveProperties(object o, string[] props)
		{
			var typ = o.GetType();
			var json = JObjectOfType(o);
			foreach (var propName in props) {
				//var field = typ.GetField(propName);
				//var val = field.GetValue(l);
				var property = typ.GetProperty(propName);
				Log.Assert(property != null, "Saving: Property {0} does not exist on type {1} (is the name correct?  Is it public?)", propName, typ);
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

		public static void LoadProperties(object o, string[] props, JToken json)
		{
			var typ = o.GetType();
			foreach (var propName in props) {
				var property = typ.GetProperty(propName);
				Log.Assert(property != null, "Loading: Property {0} not found on object of type {1}!", propName, typ);
				var loadedValue = json[propName].ToObject(property.PropertyType);
				Log.Message("Setting property {0} to {1}", property, loadedValue);
				property.SetValue(o, loadedValue);
			}
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
				var jobj = item as JToken;
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


		static bool IsJValue(Type t)
		{
			return t == typeof(long) || t == typeof(int) || t == typeof(decimal) || t == typeof(char) ||
			t == typeof(ulong) || t == typeof(float) || t == typeof(double) || t == typeof(DateTime) ||
			t == typeof(DateTimeOffset) || t == typeof(bool) || t == typeof(string) || t == typeof(Guid) ||
			t == typeof(TimeSpan) || t == typeof(Uri);
		}


		static bool IsSaveable(Type t)
		{
			return IsJValue(t) || AssetConverters.ContainsKey(t);
		}

		public static JToken Save(object o)
		{
			var typ = o.GetType();
			if (IsJValue(typ)) {
				return new JValue(o);
			} else {
				return DispatchSave(o, typ);
			}
		}

		public static object Load(JToken json)
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

		public static T Load<T>(JToken json)
		{
			if (IsJValue(typeof(T))) {
				return json.Value<T>();
			} else {
				return (T)Load(json);
			}
		}

		public static JObject JObjectOfType(object o)
		{
			var t = o.GetType();
			var json = new JObject {
				{ "type", t.ToString() },
			};
			return json;
		}


		static JToken DispatchSave(object o, Type typ)
		{
			IAssetConverter saveLoader;
			if (AssetConverters.TryGetValue(typ, out saveLoader)) {
				return saveLoader.Save(o);
			} else {
				var msg = String.Format("Could not find function to save type {0}", typ);
				throw new JsonSerializationException(msg);
			}
		}

		static object DispatchLoad(JToken o, Type typ)
		{
			IAssetConverter saveLoader;
			if (AssetConverters.TryGetValue(typ, out saveLoader)) {
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
}

