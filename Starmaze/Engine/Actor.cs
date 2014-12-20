using System;

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
	public class Actor
	{
		// Default components.
		public object PhysicsObj;
		public Renderer Renderer;

		// Other properties
		public bool Alive = true;
		public World World;
		public bool OnGround = false;
		public Actor()
		{
		}

		public virtual void Update(double dt)
		{

		}

		public virtual void OnDeath()
		{

		}
	}
}

