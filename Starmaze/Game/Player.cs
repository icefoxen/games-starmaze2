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
			Body.AddGeom(new BoxGeom(new BBox(-5, -5, 5, 5)));
			var model = Resources.TheResources.GetModel("TestModel");
			RenderParams = new StaticRendererParams(model);
			Components.Add(new KeyboardController(this));
			KeepOnRoomChange = true;
		}
	}
}

