using System;
using OpenTK;
using OpenTK.Graphics;
using Starmaze;
using Starmaze.Engine;
using Starmaze.Game;

namespace Starmaze.Content
{
	/// <summary>
	/// Actor factory.
	/// </summary>
	public static class ActFact
	{
		public static Actor Player()
		{
			var actCfg = Resources.TheResources.GetJson("player");
			var player = SaveLoad.Load<Actor>(actCfg);
			return player;
		}

		public static Actor BoxBlock(BBox bbox, Color4 color)
		{
			var act = new Actor();
			Log.Message("Creating new BoxBlock: {0} {1}", bbox, color);
			var body = new Body(bodyType: FarseerPhysics.Dynamics.BodyType.Static);
			body.Shape = Body.RectShape((float)bbox.Left, (float)bbox.Bottom, (float)bbox.Right, (float)bbox.Top);
			act.AddComponent(body);
			//Body.AddGeom(new BoxGeom(bbox));

			// BUGGO: Since the Actor gets the model and such themselves, instead of
			// it being handled by the Resources system, they aren't freed properly on game end.
			var mb = new ModelBuilder();
			var width = bbox.Dx;
			var height = bbox.Dy;
			mb.RectCorner(bbox.X0, bbox.Y0, width, height, color);
			var vertModel = mb.Finish();
			// XXX: Should we need to get a shader here?  We probably shouldn't.
			var shader = Resources.TheResources.GetShader("default");
			var model = vertModel.ToVertexArray(shader);
			act.AddComponent(new ModelRenderState(model));
			return act;
		}

		public static Actor Gate(Vector2 location, string destZone, string destRoom, Vector2d destLocation)
		{
			var act = new Actor();
			var body = new Body(location, bodyType: FarseerPhysics.Dynamics.BodyType.Static);
			act.AddComponent(body);
			var trigger = new GateTrigger(destZone, destRoom, destLocation);
			act.AddComponent(trigger);

			var mb = new ModelBuilder();
			const int width = 10;
			const int height = 10;
			mb.RectCenter(0, 0, width, height, Color4.Red);
			var vertModel = mb.Finish();
			var shader = Resources.TheResources.GetShader("default");
			var model = vertModel.ToVertexArray(shader);
			act.AddComponent(new ModelRenderState(model));

			return act;
		}
	}
}

