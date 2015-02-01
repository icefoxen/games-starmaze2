using System;
using Starmaze.Engine;

namespace Starmaze.Game
{
	public class Player : Actor, IStaticRenderable
	{
		public VertexArray Model { get; set; }
		public Player()
		{
			RenderClass = "StaticRenderer";
			Body = new Body(this);
			Body.AddGeom(new BoxGeom(new BBox(-5, -5, 5, 5)));
			Model = Resources.TheResources.GetModel("Player");
			Components.Add(new KeyboardController(this));
		}

		public override void Update(double dt)
		{
			//Console.WriteLine("Player at {0}", Body.Position);
		}

		public override void OnUpdate(object sender, EventArgs e)
		{
			Log.Message("Player updated: {0}, {1}", sender, e);
		}
	}
}

