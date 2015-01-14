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
		const double PHYSICS_HZ = 100.0;
		const double PHYSICS_FRAME_TIME = 1.0 / PHYSICS_HZ;
	}

	public class Body : Component
	{
		Vector2d Location { get; set; }

		double Rotation { get; set; }


		public Body(Actor owner) : base(owner)
		{
			Location = Vector2d.Zero;
			Rotation = 0.0;
		}
	}

	/// <summary>
	/// Contains all physics objects and handles their interactions and calculations.
	/// </summary>
	public class Space
	{
		HashSet<Body> Things;
	}
}

