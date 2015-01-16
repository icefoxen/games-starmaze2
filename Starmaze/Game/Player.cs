using System;
using Starmaze.Engine;

namespace Starmaze.Game
{
	public class Player : Actor
	{
		public Player()
		{
			RenderClass = "PlayerRenderer";
			Body = new Body(this);
		}
	}
}

