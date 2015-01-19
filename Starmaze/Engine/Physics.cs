using System;
using System.Collections.Generic;
using OpenTK;

namespace Starmaze.Engine
{
	/// <summary>
	/// A bunch of useful physics-related constants and routines.
	/// </summary>
	public static class Physics
	{
		public const double PHYSICS_HZ = 100.0;
		public const double PHYSICS_FRAME_TIME = 1.0 / PHYSICS_HZ;
	}



	public enum Facing
	{
		Left = -1,
		Right = 1,
		None = 0
	}

	public class Body : Component
	{
		public Geom Geometry;

		public Vector2d Position;
		public Vector2d Velocity;
		public Facing Facing;
		public double Rotation;
		public double Mass;
		public bool IsOnGround;
		public bool IsStationary;
		public bool IsGravitating;

		public Body(Actor owner) : base(owner)
		{
			Position = Vector2d.Zero;
			Velocity = Vector2d.Zero;
			Rotation = 0.0;
			Mass = 1.0;

			IsOnGround = false;
			IsStationary = false;
			IsGravitating = true;

			Geometry = new BoxGeom(new BBox(-5, 5, -5, 5));
		}

		public void Update(Space s, double dt)
		{
			//Console.WriteLine("Updating, {0}", dt);
			if (!IsStationary) {
				if (IsGravitating) {
					Velocity += s.Gravity * dt;
				}
				Position += Velocity * dt;
			}
		}

		public Intersection CheckCollision(Body other)
		{
			return Geometry.Intersect(other.Geometry);
		}

		public void MoveTo(Vector2d pos)
		{

			Position = pos;
		}

		public void MoveBy(Vector2d pos)
		{
			Position += pos;
		}

		// XXX: Do we translate the geometry around each time the object moves,
		// or do we
	}

	/// <summary>
	/// Contains all physics objects and handles their interactions and calculations.
	/// </summary>
	public class Space
	{
		public Vector2d Gravity;
		HashSet<Body> Bodies;

		public Space()
		{
			Gravity = -Vector2d.UnitY;
			Bodies = new HashSet<Body>();
		}

		public void Add(Body b)
		{
			Log.Assert(!Bodies.Contains(b));
			Bodies.Add(b);
		}

		public void Remove(Body b)
		{
			Log.Assert(Bodies.Contains(b));
			Bodies.Remove(b);
		}

		public void Update(double dt)
		{
			foreach (var body in Bodies) {
				body.Update(this, dt);
			}
		}
	}
}

