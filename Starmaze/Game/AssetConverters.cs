using System;
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

}

