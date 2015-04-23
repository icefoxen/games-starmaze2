using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using OpenTK;
using Starmaze.Engine;

namespace Starmaze.Game
{

	public class LifeAssetConverter : IAssetConverter
	{
		readonly string[] props = {
			"CurrentLife",
			"MaxLife",
			"DamageAttenuation",
			"DamageReduction",
		};

		public JToken Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			return SaveLoad.SaveProperties(o, props);
		}

		public object Load(JToken json)
		{
			var obj = new Life(null, 0);
			SaveLoad.LoadProperties(obj, props, json);
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

	public class TimedLifeAssetConverter : IAssetConverter
	{
		readonly string[] props = {
			"Time",
			"MaxTime",
		};

		public JToken Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			return SaveLoad.SaveProperties(o, props);
		}

		public object Load(JToken json)
		{
			var obj = new TimedLife(null, 1);
			SaveLoad.LoadProperties(obj, props, json);
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

	public class GunAssetConverter : IAssetConverter
	{
		readonly string[] props = {
			"FireOffset",
		};

		public JToken Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			return SaveLoad.SaveProperties(o, props);
		}

		public object Load(JToken json)
		{
			var obj = new Gun(null);
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

		public JToken Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			return SaveLoad.SaveProperties(o, props);
		}

		public object Load(JToken json)
		{
			var obj = new Energy(null);
			SaveLoad.LoadProperties(obj, props, json);
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

	public class InputControllerAssetConverter : IAssetConverter
	{
		public JToken Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			return SaveLoad.JObjectOfType(o);
		}

		public object Load(JToken json)
		{
			var obj = new InputController(null);
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

	// TODO: Does not save/load powers properly yet.
	public class PowerSetAssetConverter : IAssetConverter
	{
		public JToken Save(object o)
		{
			SaveLoad.PreSaveIfPossible(o);
			var powerset = o as PowerSet;
			Log.Assert(powerset != null, "Shouldn't be possible");
			var json = SaveLoad.JObjectOfType(o);
			json["PowerList"] = SaveLoad.SaveList<int>(powerset.PowerList);
			return json;
		}

		public object Load(JToken json)
		{
			var obj = new PowerSet(null);
			var components = SaveLoad.LoadList<int>(json["PowerList"].Value<JArray>());
			obj.PowerList = components.ToArray();
			SaveLoad.PostLoadIfPossible(obj);
			return obj;
		}
	}

}

