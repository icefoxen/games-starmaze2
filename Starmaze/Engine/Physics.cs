using System;
using System.Collections.Generic;
using OpenTK;
using Newtonsoft.Json;
using FarseerPhysics;
using Dyn = FarseerPhysics.Dynamics;
using Col = FarseerPhysics.Collision;
using Xna = Microsoft.Xna.Framework;

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

	public class FBody : Component
	{
		public Col.Shapes.Shape Shape { get; set; }

		public Dyn.BodyType BodyType { get; set; }

		public Dyn.Body PBody { get; set; }

		public Facing Facing { get; set; }
		// Wrappers for Body attributes
		// OPT: Aieee, wrapping and unwrapping the XNA vectors on every wossname is kinda awful.
		// Oh well, live with it for now.
		public Vector2d Position { get {
				return new Vector2d(PBody.Position.X, PBody.Position.Y);
			} set {
				PBody.Position = new Xna.Vector2((float)value.X, (float)value.Y);
			} }

		public FBody(Actor owner, Dyn.BodyType bodyType = Dyn.BodyType.Dynamic) : base(owner)
		{
			Shape = FBody.RectShape(10, 5);
			BodyType = bodyType;
		}

		public void AddToWorld(Dyn.World world)
		{
			PBody = new Dyn.Body(world, userdata: this);
			PBody.CreateFixture(Shape);
			PBody.BodyType = BodyType;
		}

		static Col.Shapes.PolygonShape RectShape(float width, float height)
		{
			var halfWidth = width / 2f;
			var halfHeight = height / 2f;
			var verts = new FarseerPhysics.Common.Vertices(4);
			// XXX: Winding?  Currently it's clockwise.
			verts.Add(new Microsoft.Xna.Framework.Vector2(-halfWidth, -halfHeight));
			verts.Add(new Microsoft.Xna.Framework.Vector2(-halfWidth, halfHeight));
			verts.Add(new Microsoft.Xna.Framework.Vector2(halfWidth, halfHeight));
			verts.Add(new Microsoft.Xna.Framework.Vector2(halfWidth, -halfHeight));
			return new Col.Shapes.PolygonShape(verts, 1f);

		}
	}

	public class Body : Component
	{
		HashSet<Geom> Geometry;
		//Custom converters are used for vector2d because otherwise circular references happen.
		public Vector2d Position { get; set; }

		public Vector2d Velocity { get; set; }

		public Facing Facing  { get; set; }

		public double Rotation  { get; set; }

		public double Mass  { get; set; }

		/// <summary>
		/// Whether or not the body is on the ground.
		/// </summary>
		public bool IsOnGround;

		/// <summary>
		/// Whether or not gravity affects the body
		/// </summary>
		public bool IsGravitating { get; set; }

		/// <summary>
		/// Whether or not the body is immobile.
		/// Immobile things _never_ move, and the immobile state never changes(?).
		/// </summary>
		public bool IsImmobile { get; set; }

		/// <summary>
		/// Force that has accumulated on the body each tick, potentially from multiple sources.
		/// </summary>
		Vector2d impulse;

		public Body(Actor owner, bool gravitating = true, bool immobile = false) : base(owner)
		{
			Position = Vector2d.Zero;
			Velocity = Vector2d.Zero;
			Rotation = 0.0;
			Mass = 1.0;

			IsOnGround = false;
			IsGravitating = gravitating;
			IsImmobile = immobile;

			Geometry = new HashSet<Geom>();
		}

		public void AddGeom(Geom geom)
		{
			Geometry.Add(geom);
		}

		public void Update(Space s, double dt)
		{

			var acceleration = impulse / Mass;
			Velocity += acceleration;
			MoveBy(Velocity * dt);
			impulse = Vector2d.Zero;
		}

		/// <summary>
		/// Accumulates a force on the object.
		/// </summary>
		/// <param name="impulse">Impulse.</param>
		public void AddImpulse(Vector2d impulse)
		{
			this.impulse += impulse;
		}

		/// <summary>
		/// Adds velocity to the body regardless of mass.
		/// </summary>
		/// <param name="velocity">Velocity.</param>
		public void AddVelocity(Vector2d velocity)
		{
			Velocity += velocity;
		}

		public Intersection CheckCollision(Body other)
		{
			foreach (var geom1 in Geometry) {
				foreach (var geom2 in other.Geometry) {
					//Log.Message("Checking collision between {0} and {1}", geom1, geom2);
					var intersection = geom1.Intersect(geom2);
					if (intersection != null) {
						//Log.Message("Collision detected");
						return intersection;
					}
				}
			}
			return null;
		}

		public void MoveTo(Vector2d pos)
		{
			var offset = Position - pos;
			Position = pos;
			foreach (var geom in Geometry) {
				geom.Translate(offset);
			}
		}

		public void MoveBy(Vector2d offset)
		{
			Position += offset;
			foreach (var geom in Geometry) {
				geom.Translate(offset);
			}
		}
		// XXX: Placeholder.
		public void HandleCollision(Body other, Intersection intersection)
		{
			Velocity = Vector2d.Zero;
			if (!IsImmobile) {
				/*
				Console.WriteLine(intersection);
				if (intersection.Normal.X != 0.0) {
					Velocity.X *= -1;
				} 
				if (intersection.Normal.Y != 0.0) {
					Velocity.Y *= -1;
				}
				var intrusionVec = intersection.Normal * intersection.Intrusion * 2;
				Position -= intrusionVec;
				*/
			}
		}
	}

	/// <summary>
	/// Contains all physics objects and handles their interactions and calculations.
	/// </summary>
	public class Space
	{
		public readonly Vector2d DEFAULT_GRAVITY = new Vector2d(0.0, -5.0);
		public Vector2d Gravity;
		HashSet<Body> Bodies;
		HashSet<Body> MovingBodies;
		HashSet<Body> ImmobileBodies;
		HashSet<Body> GravitatingBodies;

		public Space()
		{
			Gravity = DEFAULT_GRAVITY;
			Bodies = new HashSet<Body>();
			MovingBodies = new HashSet<Body>();
			ImmobileBodies = new HashSet<Body>();
			GravitatingBodies = new HashSet<Body>();
		}

		public void Add(Body b)
		{
			Log.Assert(!Bodies.Contains(b));
			Bodies.Add(b);
			if (b.IsImmobile) {
				ImmobileBodies.Add(b);
			} else {
				if (b.IsGravitating) {
					GravitatingBodies.Add(b);
				}
				MovingBodies.Add(b);
			}
		}

		public void Remove(Body b)
		{
			Log.Assert(Bodies.Contains(b));
			Bodies.Remove(b);
			if (b.IsImmobile) {
				Log.Assert(ImmobileBodies.Contains(b));
				ImmobileBodies.Remove(b);
			} else {
				if (b.IsGravitating) {
					Log.Assert(GravitatingBodies.Contains(b));
					GravitatingBodies.Remove(b);
				}
			}
		}

		public void UpdateMovingBodies(double dt)
		{
			foreach (var body in MovingBodies) {
				body.Update(this, dt);
			}
		}

		public void ApplyGravity(double dt)
		{
			foreach (var body in GravitatingBodies) {
				body.AddVelocity(DEFAULT_GRAVITY * dt);
			}
		}

		public void CheckForCollision()
		{
			// TODO:
			// Not really the most efficient way;
			// this should eventually be rewritten to use a spatial data structure or such.
			foreach (var body1 in Bodies) {
				foreach (var body2 in Bodies) {
					// Don't collide with ourselves
					if (body1 == body2) {
						continue;
					}
					var intersection = body1.CheckCollision(body2);
					if (intersection != null) {
						body1.HandleCollision(body2, intersection);
						body2.HandleCollision(body1, intersection);
					}
				}
			}
		}

		public void Update(double dt)
		{
			ApplyGravity(dt);
			CheckForCollision();
			UpdateMovingBodies(dt);
		}
	}
}

