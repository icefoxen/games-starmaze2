using System;
using System.Collections.Generic;
using Starmaze.Engine;
using OpenTK;
using OpenTK.Graphics;

namespace Starmaze.Game
{
	/// <summary>
	/// Base class for any terrain features.
	/// </summary>
	public class Terrain : Actor
	{
		public Terrain()
		{
		}
	}

	/// <summary>
	/// A standalone feature that warps you somewhere else when you near it and
	/// activate it.  Essentially a portal.  Each one is one-way, but they are generally
	/// generated in pairs.
	/// </summary>
	public class Gate : Terrain
	{
		public string DestinationZone;
		public string DestinationRoom;
		public Vector2d DestinationLocation;

		public Gate(string destZone, string destRoom, Vector2d destLocation)
		{
			DestinationZone = destZone;
			DestinationRoom = destRoom;
			DestinationLocation = destLocation;
		}
	}

	/// <summary>
	/// An axis-aligned, rectangular block of terrain
	/// </summary>
	public class BoxBlock : Terrain
	{
		public BoxBlock(BBox bbox, Color4 color)
		{
			Log.Message("Creating new BoxBlock: {0} {1}", bbox, color);
			Body = new FBody(this, bodyType: FarseerPhysics.Dynamics.BodyType.Static);
			Body.Shape = FBody.RectShape((float)bbox.Left, (float)bbox.Bottom, (float)bbox.Right, (float)bbox.Top);
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
			RenderState = new ModelRenderState(this, model);
		}
	}
}

