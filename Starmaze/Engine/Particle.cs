using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

namespace Starmaze.Engine
{
	/// <summary>
	/// A particle!  Mainly just here to contain things.
	/// </summary>
	public struct Particle
	{
		public Vector2d Position;
		public double Angle;
		public Color4 Color;
		public Vector2d Velocity;
		public double Age;

		public Particle(Vector2d pos, double angle, Color4 color, Vector2d vel, double age)
		{
			Position = pos;
			Angle = angle;
			Color = color;
			Velocity = vel;
			Age = age;
		}
	}

	/// <summary>
	/// A set of particles that tracks and updates itself.  All particles in the system obey the same rules,
	/// given by a ParticleController.
	/// </summary>
	public class ParticleGroup
	{
		public List<Particle> Particles;

		public ParticleGroup()
		{
			Particles = new List<Particle>();
		}

		public void AddParticle(Vector2d pos, double angle, Color4 color, Vector2d vel, double age = 0.0)
		{
			Particles.Add(new Particle(pos, angle, color, vel, age));
		}
	}

	/// <summary>
	/// Updates the motion and state for a ParticleGroup
	/// </summary>
	public class ParticleController
	{
		public ParticleController()
		{

		}

		public void Update(double dt, ParticleGroup group)
		{
			// OPT: This could prolly be more efficient.
			for (int i = 0; i < group.Particles.Count; i++) {
				var p = group.Particles[i];
				var vel = Vector2d.Zero;
				var pos = Vector2d.Zero;
				Vector2d.Multiply(ref p.Velocity, dt, out vel);
				Vector2d.Add(ref p.Position, ref vel, out pos);
				p.Position = pos;
				p.Age += dt;
				group.Particles[i] = p;
			}
		}
	}

	/// <summary>
	/// An object that draws a ParticleGroup.
	/// </summary>
	public class ParticleRenderer
	{
		Shader shader;
		VertexArray array;

		public ParticleRenderer()
		{
			shader = Resources.TheResources.GetShader("particle-default");
			array = Resources.TheResources.GetModel("Particle");
		}

		public void Draw(ViewManager view, ParticleGroup group)
		{
			Graphics.TheGLTracking.SetShader(shader);
			shader.UniformMatrix("projection", view.ProjectionMatrix);
			foreach (var p in group.Particles) {
				var pos = new Vector2((float)p.Position.X, (float)p.Position.Y);
				shader.Uniformf("offset", (float)p.Position.X, (float)p.Position.Y);
				shader.Uniformf("colorOffset", p.Color.R, p.Color.G, p.Color.B, p.Color.A);
				array.Draw();
			}
		}
	}

	/// <summary>
	/// A thing that regularly adds particles to a ParticleGroup.
	/// </summary>
	public class ParticleEmitter
	{
		double lastTime = 0.0;
		double nextTime = 0.0;
		double increment = 0.1;

		Random rand;

		public ParticleEmitter()
		{
			rand = new Random();
		}

		public void Update(double dt, ParticleGroup group)
		{
			lastTime += dt;
			while (lastTime >= nextTime) {
				nextTime += increment;
				var xOffset = rand.NextDouble() * 0.02;
				var yOffset = rand.NextDouble() * 0.02;

				group.AddParticle(Vector2d.Zero, 0.0, Color4.Maroon, new Vector2d(xOffset, yOffset));
			}
		}
	}

	/// <summary>
	/// A test component that emits particles.
	/// </summary>
	//class ParticleSystem : Component
	//{

	//}
}

