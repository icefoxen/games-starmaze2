using System;
using System.IO;
using OpenTK;
using OpenTK.Input;
using Newtonsoft.Json;
using System.Collections;


namespace Starmaze.Engine
{
	public class KeyBindings
	{
		//store name and the key

		public readonly Key MoveLeft, MoveRight, MoveUp, MoveDown;
		public readonly Key Fire1, Fire2, Defend;

		public KeyBindings()
		{
			MoveLeft = Key.Left;
			MoveRight = Key.Right;
			MoveUp = Key.Up;
			MoveDown = Key.Down;

			Fire1 = Key.C;
			Fire2 = Key.X;
			Defend = Key.Z;
		}

	}


	public static class Input
	{
		private static KeyBindings _keys;

		public static KeyBindings Keys {
			get {
				Log.Assert(_keys != null, "Tried to get key bindings that are not initialized!");
				return _keys;
			}
			set {
				_keys = value;
			}
		}

		public static void Init(KeyBindings bindings)
		{
			Keys = bindings;
		}
	}

}
