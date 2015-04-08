using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Starmaze.Engine
{
	/// <summary>
	/// An area of the game world consisting of a set of rooms all sharing a theme, music, color scheme,
	/// and other characteristics.
	/// </summary>
	public class Zone
	{
		public string Name { get; set; }

		public Dictionary<string, Room> Rooms { get; set; }

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

		public IEnumerable<Room> GetRooms()
		{
			return Rooms.Values;
		}

		/// <summary>
		/// An indexer to make it easier to retrieve rooms.  Just go zone[roomname].
		/// </summary>
		/// <param name="room">Room name.</param>
		public Room this [string room] {
			get {
				Log.Assert(Rooms.ContainsKey(room), "Zone {0} does not contain room {1}!", Name, room);
				return Rooms[room];
			}
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
		//public Zone Zone;
		public string Name { get; set; }

		public IEnumerable<Actor> Actors { get; set; }
		//IEnumerable<Actor> StaticFrozen;
		//IEnumerable<Actor> DynamicFrozen;
		//IEnumerable<Actor> DynamicLive;
		//public Room(string name, Zone zone, IEnumerable<Actor> actors)
		public Room(string name, IEnumerable<Actor> actors)
		{
			Name = name;
			//Zone = zone;
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
}

