using System;
using System.Collections.Generic;

namespace Starmaze.Game
{
	public class Room
	{

	}

	/// <summary>
	/// This is the object that tracks all rooms, zones, etc, and generally keeps track of
	/// where things are and how they connect together.
	/// </summary>
	public class WorldMap
	{
		HashSet<Room> Rooms;

		public WorldMap()
		{
			Rooms = new HashSet<Room>();
		}
	}
}

