using System;
using OpenTK;

namespace Starmaze.Engine
{
	/// <summary>
	/// Base class for all Components.  Actors are made of Components.
	/// </summary>
	public class Component
	{
		public Actor Owner;

		public Component(Actor owner)
		{
			Owner = owner;
		}
	}

	/// <summary>
	/// An Actor is any object that exists in the game world.
	/// </summary>
	public class Actor : IComparable<Actor>
	{
		// Components.
		public Body Body;
		public Component Controller;
		// Other properties
		readonly long OrderingNumber;
		public string RenderClass;
		public bool Alive = true;
		public bool KeepOnRoomChange = false;
		// Used for StaticRenderer
		public VertexArray Model;
		// XXX: Dependency inversion
		public Starmaze.Game.World World;

		public Actor()
		{
			RenderClass = "TestRenderer";
			OrderingNumber = Util.GetSerial();
			Model = null;
		}

		public virtual void Update(double dt)
		{
		}

		public virtual void OnDeath()
		{

		}
		// XXX: Dependency inversion.
		public virtual void ChangeRoom(Starmaze.Game.Room oldRoom, Starmaze.Game.Room newRoom)
		{

		}

		/// <summary>
		/// Provides an ordering to Actors, allowing them to be sorted consistently, so that the rendering
		/// code always draws them in the same order.
		/// </summary>
		/// <returns>The to.</returns>
		/// <param name="other">Other Actor.</param>
		public int CompareTo(Actor other)
		{
			return OrderingNumber.CompareTo(other.OrderingNumber);
		}
	}
}

