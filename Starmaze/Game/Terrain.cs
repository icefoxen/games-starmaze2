using System;
using System.Collections.Generic;
using Starmaze.Engine;
using OpenTK.Graphics;

namespace Starmaze.Game
{
	/// <summary>
	/// An area of the game world consisting of a set of rooms all sharing a theme, music, color scheme,
	/// and other characteristics.
	/// </summary>
	public class Zone
	{
		public string Name;
	}

	/// <summary>
	/// A named area that connects to other Areas.
	/// 
	/// Static actors are reloaded from a frozen description every time the room is re-entered,
	/// and discarded when the room is left.  Dynamic actors preserve their state.
	/// </summary>
	public class Room
	{
		public Zone Zone;
		public string Name;
		//IEnumerable<Actor> StaticFrozen;
		//IEnumerable<Actor> DynamicFrozen;
		//IEnumerable<Actor> DynamicLive;
		/// <summary>
		/// Creates all the Actors actually in the room.
		/// </summary>
		/// <returns>The actors in the room.</returns>
		public IEnumerable<Actor> ReifyActorsForEntry()
		{
			return new List<Actor>();
		}
	}

	/// <summary>
	/// This is the object that tracks all rooms, zones, etc, and generally keeps track of
	/// where things are and how they connect together.
	/// </summary>
	public class WorldMap
	{
		Dictionary<string, Zone> zones;
		Dictionary<string, Room> rooms;

		public WorldMap()
		{
			zones = new Dictionary<string,Zone>();
			rooms = new Dictionary<string,Room>();
		}
	}

	/// <summary>
	/// Base class for any terrain features.
	/// </summary>
	public class Terrain : Actor
	{
		Room room;

		public Terrain(Room room)
		{
			this.room = room;
		}
	}

	/// <summary>
	/// An axis-aligned, rectangular block of terrain
	/// </summary>
	public class BoxBlock : Terrain
	{
		public BoxBlock(Room room, BBox bbox, Color4 color) : base(room)
		{
			Body = new Body(this, immobile: true);
			Body.AddGeom(new BoxGeom(bbox));
			
			RenderClass = "StaticRenderer";
			// BUGGO: Since the Actor gets the model and such themselves, instead of
			// it being handled by the Resources system, they aren't freed properly on game end.
			var mb = new ModelBuilder();
			var width = bbox.Right - bbox.Left;
			var height = bbox.Top - bbox.Bottom;
			mb.RectCorner(bbox.Left, bbox.Bottom, width, height, Color4.Blue);
			var vertModel = mb.Finish();
			// XXX: Should we need to get a shader here?  We probably shouldn't.
			var shader = Resources.TheResources.GetShader("default");
			Model = vertModel.ToVertexArray(shader);
		}
	}
}

