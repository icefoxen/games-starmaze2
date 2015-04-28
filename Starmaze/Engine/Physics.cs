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

	/// <summary>
	/// A physics component using the Farseer physics engine.
	/// See http://farseerphysics.codeplex.com/ for more info on *that*.
	/// </summary>
	public class Body : Component
	{
		protected static readonly Dyn.World DummyWorld = new Dyn.World(Xna.Vector2.Zero);

		public Col.Shapes.Shape Shape { get; set; }

		public Dyn.BodyType BodyType { get; set; }

		public Dyn.Body PBody { get; set; }

		public Dyn.Fixture Fixture { get; set; }

		public Facing Facing { get; set; }
		// Wrappers for Body attributes
		// OPT: Aieee, wrapping and unwrapping the XNA vectors on every wossname is kinda awful.
		// Oh well, live with it for now.
		public Vector2 Position { 
			get {
				return Util.ConvertVector2(PBody.Position);
			}
			set {
				PBody.Position = Util.ConvertVector2(value);
			} 
		}

		public Vector2 Velocity {
			get {
				return Util.ConvertVector2(PBody.LinearVelocity);
			}
			set {
				PBody.LinearVelocity = Util.ConvertVector2(value);
			} 
		}

		public double Rotation {
			get {
				return (double)PBody.Rotation;
			}
			set {
				PBody.Rotation = (float)value;
			} 
		}

		public Body(Vector2? position = null, Dyn.BodyType bodyType = Dyn.BodyType.Dynamic) : base()
		{
			Shape = Body.RectShape(10, 20);
			BodyType = bodyType;

			var pos = position ?? Vector2.Zero;
			Xna.Vector2 xpos = Util.ConvertVector2(pos);
			PBody = new Dyn.Body(DummyWorld, position: xpos, userdata: this);
			Fixture = PBody.CreateFixture(Shape, userData: this);
			PBody.BodyType = BodyType;
		}

		/// <summary>
		/// AddToWorld() and RemoveFromWorld() are necessary to make a square peg fit in a round hole, essentially.
		/// See, we're going to want to have actors with Body's attached to them that aren't in the current World.
		/// For instance, when they're representing a room that hasn't been entered yet.
		/// Or when they've just been loaded and we're about to create them but still need to know where the heck
		/// they are.
		/// Since Farseer makes it impossible to have a Body that doesn't have a World attached to it, we make a dummy
		/// World and use that to initialize Body's, then replace it with a copy that refers to the real game's World 
		/// when the actor goes live.
		/// OPT: This allocates, irritatingly, but won't be happening every frame.
		/// </summary>
		/// <param name="world">World.</param>
		public void AddToWorld(Dyn.World world)
		{
			PBody = PBody.Clone(world: world);
			Fixture = PBody.CreateFixture(Shape);
			Log.Message("Added body to world, type {0}, actor {1}", PBody.BodyType, Owner);
		}

		public void RemoveFromWorld(Dyn.World world)
		{
			world.RemoveBody(PBody);
			PBody = PBody.Clone(world: DummyWorld);
		}

		public static Col.Shapes.PolygonShape RectShape(float width, float height)
		{
			var halfWidth = width / 2f;
			var halfHeight = height / 2f;
			return Body.RectShape(-halfWidth, -halfHeight, halfWidth, halfHeight);
		}

		public static Col.Shapes.PolygonShape RectShape(float x0, float y0, float x1, float y1)
		{
			var verts = new FarseerPhysics.Common.Vertices {
				new Microsoft.Xna.Framework.Vector2(x0, y0),
				new Microsoft.Xna.Framework.Vector2(x0, y1),
				new Microsoft.Xna.Framework.Vector2(x1, y1),
				new Microsoft.Xna.Framework.Vector2(x1, y0),
			};
			return new Col.Shapes.PolygonShape(verts, 1f);
		}

		public override string ToString()
		{
			return string.Format("[Body: Shape={0}, BodyType={1}, PBody={2}, Facing={3}, Position={4}, Velocity={5}, Rotation={6}]", Shape, BodyType, PBody, Facing, Position, Velocity, Rotation);
		}
	}
}

