using System;
using System.IO;
using OpenTK;
using OpenTK.Input;
using Newtonsoft.Json;
using System.Collections;

namespace Starmaze.Engine
{
	public enum InputAction
	{
		Unbound,
		MoveLeft,
		MoveRight,
		MoveUp,
		MoveDown,
		Fire1,
		Fire2,
		Defend,
		Jump,
        ToggleFPS
	}

	public class KeyConfig
	{
		//store name and the key
		public readonly Key MoveLeft, MoveRight, MoveUp, MoveDown;
        public readonly Key Fire1, Fire2, Defend, ToggleFPS;

		public KeyConfig()
		{
			MoveLeft = Key.Left;
			MoveRight = Key.Right;
			MoveUp = Key.Up;
			MoveDown = Key.Down;

			Fire1 = Key.C;
			Fire2 = Key.X;
			Defend = Key.Z;
            ToggleFPS = Key.F1;
		}
	}

	public class KeyboardBinding
	{
		public InputAction[] Keys;

		public KeyboardBinding(KeyConfig config)
		{
			// Array gets filled with default(InputAction) by default, which is equivalent to (InputAction)0 ,
			// which is the first item in the enum, which is InputAction.Unbound
			Keys = new InputAction[(int)Key.LastKey];
			Keys[(int)config.MoveLeft] = InputAction.MoveLeft;
			Keys[(int)config.MoveRight] = InputAction.MoveRight;
			Keys[(int)config.MoveUp] = InputAction.MoveUp;
			Keys[(int)config.MoveDown] = InputAction.MoveDown;
			
			Keys[(int)config.Fire1] = InputAction.Fire1;
			Keys[(int)config.Fire2] = InputAction.Fire2;
			Keys[(int)config.Defend] = InputAction.Defend;
            Keys[(int)config.ToggleFPS] = InputAction.ToggleFPS;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
		public InputAction Action(Key key)
		{
			return Keys[(int)key];
		}
	}
}
