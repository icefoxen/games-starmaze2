using System;
using System.Collections.Generic;
using Starmaze.Engine;
using OpenTK;
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
		public Dictionary<string, Room> Rooms;

		/// <summary>
		/// An indexer to make it easier to retrieve rooms.  Just go Zone[roomname].
		/// </summary>
		/// <param name="room">Room name.</param>
		public Room this [string room] {
			get {
				Log.Assert(Rooms.ContainsKey(room), "Zone {0} does not contain room {1}!", Name, room);
				return Rooms[room];
			}
		}

		public Zone(string name)
		{
			Name = name;
			Rooms = new Dictionary<string, Room>();
		}

		public Zone(string name, IEnumerable<Room> rooms) : this(name)
		{
			foreach (var room in rooms) {
				AddRoom(room);
			}
		}

		public void AddRoom(Room room)
		{
			Rooms.Add(room.Name, room);
		}
	}

	/// <summary>
	/// A named area that connects to other Rooms.
	/// 
	/// Static actors are reloaded from a frozen description every time the room is re-entered,
	/// and discarded when the room is left.  Dynamic actors preserve their state.
	/// 
	/// TODO: For now none of that is implemented, we just keep a list of actors.
	/// </summary>
	public class Room
	{
		public Zone Zone;
		public string Name;
		IEnumerable<Actor> Actors;
		//IEnumerable<Actor> StaticFrozen;
		//IEnumerable<Actor> DynamicFrozen;
		//IEnumerable<Actor> DynamicLive;

		public Room(string name, Zone zone, IEnumerable<Actor> actors)
		{
			Name = name;
			Zone = zone;
			Actors = actors;
		}

		/// <summary>
		/// Returns all the Actors actually in the room.
		/// </summary>
		/// <returns>The actors in the room.</returns>
		public IEnumerable<Actor> ReifyActorsForEntry()
		{
			return Actors;
		}

		/// <summary>
		/// Saves the state of any actor that persists when you leave and re-enter the Room.
		/// </summary>
		public void SaveActorsOnExit()
		{

		}
	}

	/// <summary>
	/// This is the object that tracks all rooms, zones, etc, and generally keeps track of
	/// where things are and how they connect together.
	/// </summary>
	public class WorldMap
	{
		Dictionary<string, Zone> Zones;

		/// <summary>
		/// An indexer to make it easier to retrieve zones.  Just go WorldMap[zonename]
		/// </summary>
		/// <param name="zone">Zone name.</param>
		public Zone this [string zone] {
			get {
				Log.Assert(Zones.ContainsKey(zone), "Zone {0} does not exist!", zone);
				return Zones[zone];
			}
		}

		public WorldMap()
		{
			Zones = new Dictionary<string,Zone>();
		}

		public void AddZone(Zone zone)
		{
			Zones.Add(zone.Name, zone);
		}
	}

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
			Body = new Body(this, immobile: true);
			Body.AddGeom(new BoxGeom(bbox));
			

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
			RenderSpecification = new StaticRenderSpec(model);
		}
	}
}

