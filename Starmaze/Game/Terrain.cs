using System;
using System.Collections.Generic;
using Starmaze.Engine;
using OpenTK;
using OpenTK.Graphics;
using FarseerPhysics.Dynamics;

namespace Starmaze.Game
{

	/// <summary>
	/// A standalone feature that warps you somewhere else when you near it and
	/// activate it.  Essentially a portal.  Each one is one-way, but they are generally
	/// generated in pairs.
	/// </summary>
	public class GateTrigger : Component
	{
		public string DestinationZone;
		public string DestinationRoom;
		public Vector2d DestinationLocation;

		public GateTrigger(string destZone, string destRoom, Vector2d destLocation) : base()
		{
			DestinationZone = destZone;
			DestinationRoom = destRoom;
			DestinationLocation = destLocation;
			HandledEvents = EventType.OnCollision | EventType.OnSeparation;
		}

		public override bool OnCollision(Fixture f1, Fixture f2, FarseerPhysics.Dynamics.Contacts.Contact contact)
		{
			return base.OnCollision(f1, f2, contact);
		}

		public override void OnSeparation(Fixture f1, Fixture f2)
		{
			base.OnSeparation(f1, f2);
		}
	}
}

