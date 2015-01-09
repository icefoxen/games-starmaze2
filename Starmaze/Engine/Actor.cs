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
		// Default components.
		public Vector2d Position;
		public object PhysicsObj;
		public string RenderClass;
		// Other properties
		public bool Alive = true;
		public World World;
		public bool OnGround = false;
		public readonly long OrderingNumber;

		public Actor()
		{
			RenderClass = "TestRenderer";
			OrderingNumber = Util.GetSerial();
		}

		public virtual void Update(double dt)
		{
			Position += new Vector2d(dt, dt);
		}

		public virtual void OnDeath()
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

