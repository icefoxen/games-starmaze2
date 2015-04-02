using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

namespace Starmaze.Engine
{
	/*
	public class ParticleInfo
	{
		Color4[] Color;
		double[] Opacity;
		double[] Size;
		double[] GravityVel;
		double[] FadeRate;
		double[] MaxLifespan;
		double[] Speed;
		double[] Elasticity;
		double[] Floor;
	}

	public class ParticleSet
	{
		ParticleInfo[] Set;
	}

	public class ParticleSystem
	{
		Random Rand;
		List<Particle> Particles;

		public ParticleSystem()
		{
			Rand = new Random();
			Particles = new List<Particle>();
		}

		public void CreateParticles(ParticleSet type, double x, double y, int count, double radius = 0)
		{
			// OPT: Could be optimized further for radius == 0 case, but, meh
			for (int i = 0; i < count; i++) {
				var currentX = x - radius + Rand.NextDouble() * radius * 2;
				var currentY = y - radius + Rand.NextDouble() * radius * 2;

			}
		}

		public void Update(double dt)
		{

		}

		public void Draw()
		{

		}

		  
		/// <summary>
		/// Go through and remove dead particles, replacing them with ones off the end
		/// of the list.
		/// </summary>
		void PurgeDeadParticles()
		{

		}

		T ArrayRand<T>(IList<T> items)
		{

		}
	}

	public class Particle
	{
		Vector2d InitialPosition;
		Vector2d Position;
		Vector2d Velocity;

		public Particle(Vector2d pos, Vector2d vel, ParticleInfo type)
		{

			InitialPosition = pos;
			Position = pos;
			Velocity = vel;
		}

		public Particle(Vector2d pos, Vector2d vel, ParticleSet type)
		{

		}
	}
	*/
	/// <summary>
	/// A particle!  Mainly just here to contain things.
	/// </summary>
	public struct Particle
	{
		public Vector2d Position;
		public double Angle;
		public Color4 Color;
		public Vector2d Velocity;
		public double Life;
		readonly double MaxLife;

		public Particle(Vector2d pos, double angle, Color4 color, Vector2d vel, double life)
		{
			Position = pos;
			Angle = angle;
			Color = color;
			Velocity = vel;
			MaxLife = life;
			Life = life;
		}
	}

	/// <summary>
	/// A set of particles that tracks and updates itself.  All particles in the system obey the same rules,
	/// given by a ParticleController.
	/// </summary>
	// XXX: Removing particles while maintaining draw order might become a little wonky.
	public class ParticleGroup
	{
		const int DefaultParticleCache = 1024;
		public List<Particle> Particles;

		public ParticleGroup(int cacheSize = DefaultParticleCache)
		{
			Particles = new List<Particle>(cacheSize);
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

		public void Update(double dt,ref List<Particle> list)
		{
			// OPT: This could prolly be more efficient.
			// But a core i5 handles 50k particles without much sweat, so, no sweat.
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
				var vel = Vector2d.Zero;
				var pos = Vector2d.Zero;
				Vector2d.Multiply(ref p.Velocity, dt, out vel);
				Vector2d.Add(ref p.Position, ref vel, out pos);
				p.Position = pos;
				p.Life -= dt;
                list[i] = p;
			}
		}
	}

	/// <summary>
	/// An object that draws a ParticleGroup.
	/// </summary>
	

	/// <summary>
	/// A thing that regularly adds particles to a ParticleGroup.
	/// </summary>
	public class ParticleEmitter
	{
		double lastTime = 0.0;
		double nextTime = 0.0;
		double emitDelay;
		Random rand;

        const int DefaultParticleCache = 1024;
		public List<Particle> Particles;

        public ParticleEmitter(double emitDelay = 0.1, int cacheSize = DefaultParticleCache)
        {
            rand = new Random();
            this.emitDelay = emitDelay;
            Particles = new List<Particle>(cacheSize);
        }

		public void AddParticle(Vector2d pos, double angle, Color4 color, Vector2d vel, double age = 0.0)
		{
			Particles.Add(new Particle(pos, angle, color, vel, age));
		}		

		public void Update(double dt)
		{
			lastTime += dt;
			while (lastTime >= nextTime) {
				nextTime += emitDelay;
				var xOffset = rand.NextDouble() * 0.02;
				var yOffset = rand.NextDouble() * 0.02;

				AddParticle(Vector2d.Zero, 0.0, Color4.Maroon, new Vector2d(xOffset, yOffset));
			}
		}
	}

	/// <summary>
	/// A test component that emits particles.
	/// </summary>
	// XXX: Doesn't draw anything yet...  How exactly to integrate the ParticleRenderer
	// with the Renderer pipeline is something I need to think about.
	class ParticleComponent : Component
	{

/*		ParticleGroup group;
		ParticleEmitter emitter;
		ParticleController controller;
        */
        Actor actor;

		public ParticleComponent(Actor owner, World world, double emitDelay = 0.1) : base(owner)
		{
			HandledEvents = EventType.OnUpdate;

			/*group = new ParticleGroup();
			emitter = new ParticleEmitter(emitDelay: emitDelay);
			controller = new ParticleController();*/
            actor = new Actor();
            actor.Body = owner.Body;
            ParticleRenderState renderstate = new ParticleRenderState(actor, emitDelay);
            actor.RenderState = renderstate;
            world.AddActor(actor);
		}

		public override void OnUpdate(object sender, FrameEventArgs e)
		{
            var dt = e.Time;
            ParticleEmitter emitter = ((ParticleRenderState)actor.RenderState).emitter;
            emitter.Update(dt);
            ((ParticleRenderState)actor.RenderState).controller.Update(dt, ref emitter.Particles);
			//emitter.Update(dt, group);
			//controller.Update(dt, group);
		}
	}
}

