using System;
using Starmaze.Engine;

namespace Starmaze.Game
{
	public class Player : Actor
	{
		public Player()
		{
			RenderClass = "StaticRenderer";
			Body = new Body(this);
			Model = Resources.TheResources.GetModel("Player");
		}
	}
}

