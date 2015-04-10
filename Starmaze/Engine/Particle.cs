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
		public float scale;

		public Particle(Vector2d pos, double angle, Color4 color, Vector2d vel, double life, float s=1f)
		{
			Position = pos;
			Angle = angle;
			Color = color;
			Velocity = vel;
			MaxLife = life;
			Life = life;
			scale = s;
           
		}
	}

	/// <summary>
	/// Updates the motion and state for a  List of Particles. Handles the Logic
	/// </summary>
	public class ParticleController
	{
		public Vector2d position;
		public float maxLifeTime = 1.0f;
		public float startScale = 1.0f;
		public float deltaScale = 0.0f;
		public Vector2d velocity;

		public ParticleController()
		{
			position = Vector2d.Zero;
			maxLifeTime = 1f;
			startScale = 1f;
			deltaScale = 0f;
			velocity = Vector2d.One;
		}

		public ParticleController(Vector2d _pos, Vector2d _Velocity,
		                          float _maxLifeTime=1f, float _startScale=1f, float _deltaScale=0f)
		{
			position = _pos;
			velocity = _Velocity;
			maxLifeTime = _maxLifeTime;
			startScale = _startScale;
			deltaScale = _deltaScale;
		}

		public void Update(double dt, ref List<Particle> list)
		{
			// OPT: This could prolly be more efficient.
			// But a core i5 handles 50k particles without much sweat, so, no sweat.
			Random rand = new Random();
			for (int i = 0; i < list.Count; i++) {
				var p = list[i];
				var vel = velocity * rand.NextDouble() * dt;
				var pos = position;
				Vector2d.Multiply(ref p.Velocity, ref vel, out vel);
				Vector2d.Add(ref p.Position, ref vel, out pos);
				p.Position = pos;
				p.Life -= dt;
				list[i] = p;
			}
			// Log.Message(String.Format("Particle ({0}) V {0}", list[0].Position, list[0].Velocity));
		
		}
	}

	/// <summary>
	/// An object that draws a ParticleGroup.
	/// </summary>
	

	/// <summary>
	/// A thing that regularly adds particles to a List of Particles.
	/// </summary>
	public class ParticleEmitter
	{
		//Properties for 
		double lastTime = 0.0;
		double nextTime = 0.0;
		double emitDelay;
		Random rand;
		private int maxParticles;
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
			//Log.Message(String.Format("Particle ({0}) V {0}", pos, vel));
		}

		public void Update(double dt)
		{
			lastTime += dt;
			while (lastTime >= nextTime) {
				nextTime += emitDelay;
				var xOffset = rand.NextDouble() * 0.2;
				var yOffset = rand.NextDouble() * 0.2;

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

		Actor actor;
		ParticleController controller;
		public Vector2d position;
		public float maxLifeTime = 1.0f;
		public float startScale = 1.0f;
		public float deltaScale = 0.0f;
		public Vector2d velocity;

		public ParticleComponent(Actor owner, World world, Vector2d _velocity, double emitDelay = 0.1,
		                         float _maxLifeTime=1f, float _startScale=1f, float _deltaScale=0f) : base(owner)
		{
			HandledEvents = EventType.OnUpdate;
			actor = new Actor();
			actor.Body = new FBody(actor);

			velocity = _velocity;
			maxLifeTime = _maxLifeTime;
			startScale = _startScale;
			deltaScale = _deltaScale;

			//controller = new ParticleController(owner.Body.Position, velocity, maxLifeTime, startScale, deltaScale);

			Texture texture = Resources.TheResources.GetTexture("dot");
			ParticleRenderState renderstate = new ParticleRenderState(actor, texture, emitDelay, 0f, new Vector2(0.1f, 0.1f));
			actor.RenderState = renderstate;
			world.AddActor(actor);

            
		}

		public override void OnUpdate(object sender, FrameEventArgs e)
		{
			var dt = e.Time;
			ParticleEmitter emitter = ((ParticleRenderState)actor.RenderState).emitter;
			emitter.Update(dt);
			controller.Update(dt, ref emitter.Particles);
		}

		public int particleCount()
		{
			return ((ParticleRenderState)actor.RenderState).emitter.Particles.Count;
		}
	}
}

