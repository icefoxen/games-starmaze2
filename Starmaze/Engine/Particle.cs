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
		//Life times are doubles
		//scales are
		public Vector2d Position;
		public double velocityMagnitude;
		public Color4 Color;
		public Vector2d Angle;
		public double Life;
		public readonly double MaxLife;
		public float scale;
		public Vector2d velocity;


		public Particle(Vector2d pos, double magnitude, Color4 color, Vector2d angle, double life = 5f, float s = 1f)
		{
			Position = pos;
			velocityMagnitude = magnitude;
			Color = color;
			Angle = angle;
			MaxLife = life;
			Life = life;
			scale = s;
			velocity = Angle * velocityMagnitude;
		}
	}


	/// <summary>
	/// Updates the motion and state for a  List of Particles. Handles the Logic
	/// </summary>
	public class ParticleController
	{
		private Vector2d position;
		private float startScale = 1.0f;
		//add scaling over time begining at start scale
		private float deltaScale = 0.0f;
		//add scaling over time by delta scale
		private float gravity = 1.0f;
        public bool fadeWithTime = false;

		public ParticleController()
		{
			position = Vector2d.Zero;
			startScale = 1f;
			deltaScale = 0f;
		}

		public ParticleController(Vector2d position, double velocity_Magnitude, float gravity = 1.0f, float startScale = 1f, float deltaScale = 0f)
		{
			this.position = position;
			this.startScale = startScale;
			this.deltaScale = deltaScale;
			this.gravity = gravity;
		}

		public void Update(double dt, ref ParticleGroup group)
		{
			// OPT: This could prolly be more efficient.
			// But a core i5 handles 50k particles without much sweat, so, no sweat.
            List<Particle> list = group.Particles;
            Color4 newColor;
            float fadeRate;
			for (int i = list.Count - 1; i > -1; i--) {
                
                var p = list[i];

                if (fadeWithTime)
                {
                    fadeRate=(float)(dt/p.MaxLife);
                    newColor = new Color4(p.Color.R -fadeRate, p.Color.G - fadeRate, p.Color.B - fadeRate, p.Color.A);
                    p.Color = newColor;
                }

				//***Calculate the factors that will affect velocty***
				//1. Applying gravity
				var _gravity = -1 * Vector2d.UnitY * (gravity);
				p.velocity += _gravity * dt;
				//*(velocity * rand.NextDouble() * dt);
				var speed = p.velocity * dt;
				var pos = Vector2d.Zero;
				//Vector2d.Multiply(ref vel, ref _gravity, out vel);

				//Final Step - add the calculated Velocity the particle's position
				Vector2d.Add(ref p.Position, ref speed, out pos);
				p.Position = pos;
				p.Life -= dt;
				list[i] = p;

              

			    if(p.Life<0)
                    list=group.Remove(i);
			}
			// Log.Message(String.Format("Particle ({0}) V {0}", list[0].Position, list[0].Velocity));

		}
	}

	/// <summary>
	/// The Base class for managing how the particles are first emmitted and in what kind of shape they emitt as
	/// </summary>
	abstract public class ParticleEmitter
	{
		//Emitter Properties
		protected double lastTime = 0.0;
		protected double nextTime = 0.0;
		protected Random rand;
		protected double emitDelay, velocityMagnitude, maxLifeTime;
		public List<Particle> Particles;
        public Color4 color;

		public abstract void Update(double dt, ref ParticleGroup particle_group);

	}

	/// <summary>
	/// 
	/// </summary>
	public class CircleEmitter : ParticleEmitter
	{
		public float radius;
		public int start_angle, end_angle, current_angle;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="color"></param>
		/// <param name="velocityMagnitude"></param>
		/// <param name="emitDelay"></param>
		/// <param name="MaxParticles"></param>
		/// <param name="maxLifeTime"></param>
		public CircleEmitter(Color4 color, float radius = 1f, int start_angle = 0, int end_angle = 360, double velocityMagnitude = 1f, double emitDelay = 0.1, int MaxParticles = 1024, double maxLifeTime = 1f)
		{
			//Particle Emitter Properties
			rand = new Random();
			this.color = color;
			this.velocityMagnitude = velocityMagnitude;
			this.emitDelay = emitDelay;
			this.maxLifeTime = maxLifeTime;
			this.radius = radius;
			this.start_angle = start_angle;
			current_angle = start_angle;
			this.end_angle = end_angle;
			Particles = new List<Particle>(MaxParticles);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dt"></param>
        public override void Update(double dt, ref ParticleGroup particle_group)
		{
			Vector2d position = Vector2d.Zero, angleVec = Vector2d.Zero;

			//current_angle = start_angle * Math.PI / 180;

			lastTime += dt;
			for (int i = 0; i <= 360 && lastTime >= nextTime; i += 10, current_angle++) {			//while (lastTime >= nextTime)

				if (current_angle > end_angle - 1) {
					current_angle = start_angle;
					nextTime += emitDelay;
					break;
				}

				//angle = random.Next(start_angle, end_angle);
				position = new Vector2d(radius * Math.Cos(current_angle * Math.PI / 180), radius * Math.Sin(current_angle * Math.PI / 180));
				Vector2d.Normalize(ref position, out angleVec);
				//Log.Message(String.Format("Particle Angle {0} , {1} , {2}", current_angle, position.X, position.Y));
                particle_group.AddParticle(position, velocityMagnitude, color, angleVec, maxLifeTime);
			}
		}

	}

	/// <summary>
	/// 
	/// </summary>
	public class LineEmitter : ParticleEmitter
	{
		public float length;
		public int angle;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="color"></param>
		/// <param name="velocityMagnitude"></param>
		/// <param name="emitDelay"></param>
		/// <param name="MaxParticles"></param>
		/// <param name="maxLifeTime"></param>
		/// <param name="length"></param>
		/// <param name="length"></param>
		/// <param name="end_angle"></param>
		public LineEmitter(Color4 color, float length = 1f, int angle = 0,double velocityMagnitude = 1f, double emitDelay = 0.1, double maxLifeTime = 1f)
		{
			//Particle Emitter Properties
			rand = new Random();
			this.color = color;
			this.velocityMagnitude = velocityMagnitude;
			this.emitDelay = emitDelay;
			this.maxLifeTime = maxLifeTime;
			this.length = length;
			this.angle = angle;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dt"></param>
		public override void Update(double dt, ref ParticleGroup particle_group)
		{
			Vector2d position = Vector2d.Zero, angleVec = Vector2d.Zero;

			//current_angle = start_angle * Math.PI / 180;

			lastTime += dt;
			//for (int i = 0; i <= 360 && lastTime >= nextTime; i += 10, current_angle++)
			while (lastTime >= nextTime) {
				nextTime += emitDelay; 
				position = new Vector2d( Math.Cos(angle * Math.PI / 180),  Math.Sin(angle * Math.PI / 180));
				Vector2d.Normalize(ref position, out angleVec);
				//Log.Message(String.Format("Particle Angle {0} , {1} , {2}", current_angle, position.X, position.Y));
                particle_group.AddParticle(position, velocityMagnitude, color, angleVec, maxLifeTime);
			}
		}

	}

    public class PointEmitter : ParticleEmitter
    {
        public float length;
        public int angle;
        public Vector2d velocity;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <param name="velocityMagnitude"></param>
        /// <param name="emitDelay"></param>
        /// <param name="MaxParticles"></param>
        /// <param name="maxLifeTime"></param>
        /// <param name="length"></param>
        /// <param name="length"></param>
        /// <param name="end_angle"></param>
        public PointEmitter(Color4 color, double xVelocity = 1f, double yVelocity = 1f, double emitDelay = 0.1, double maxLifeTime = 1f)
        {
            //Particle Emitter Properties
            rand = new Random();
            this.color = color;
            this.emitDelay = emitDelay;
            this.maxLifeTime = maxLifeTime;
            this.velocity = new Vector2d(xVelocity, yVelocity);
            velocityMagnitude = Math.Sqrt(xVelocity * xVelocity + yVelocity * yVelocity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        public override void Update(double dt, ref ParticleGroup particle_group)
        {
            double xV = rand.NextDouble() * velocity.X*(rand.NextDouble()*-1);
            double yV = rand.NextDouble() * velocity.Y;
            //Will Add Code Soon
            Vector2d position = Vector2d.Zero, angleVec = new Vector2d(xV,yV);
         
            //current_angle = start_angle * Math.PI / 180;

            lastTime += dt;
            //for (int i = 0; i <= 360 && lastTime >= nextTime; i += 10, current_angle++)
            while (lastTime >= nextTime)
            {
                nextTime += emitDelay;
                //position = new Vector2d(Math.Cos(angle * Math.PI / 180), Math.Sin(angle * Math.PI / 180));
                Vector2d.Normalize(ref angleVec, out angleVec);
                particle_group.AddParticle(position, velocityMagnitude, color, angleVec, maxLifeTime);
                
            } /*
               * Need velocuty magnitude
               * need position
               */
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class ParticleGroup
    {

        public List<Particle> Particles;

        public ParticleGroup(int MaxParticles)
        {

            Particles = new List<Particle>(MaxParticles);

        }

        public void AddParticle(Vector2d pos, double velocityMagnitude, Color4 color, Vector2d angle, double age = 1.0)
        {
            Particles.Add(new Particle(pos, velocityMagnitude, color, angle, age));
            //Log.Message(String.Format("Particle ({0}) V {0}", pos, vel));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i">the Index to remove at</param>
        /// <returns>the updated particle list with the removed item</returns>
        public List<Particle> Remove(int i)
        {
            Particles[i] = Particles[Particles.Count - 1];
            Particles.RemoveAt(Particles.Count - 1);
            return Particles;
        }

    }
	/// <summary>
	/// An object that draws a ParticleGroup.
	/// </summary>

	/// <summary>
	/// A component that emits particles.
	/// </summary>
	// XXX: Exactly to integrate the ParticleRenderer with the Renderer pipeline is something I need to think about.
	class ParticleComponent : Component
	{
		Actor actor;
		ParticleController controller;
		ParticleEmitter emitter;
        ParticleGroup particle_group;

        //public CircleEmitter(Color4 color, double velocityMagnitude = 1f, double emitDelay = 0.1, int MaxParticles = 1024, double maxLifeTime = 1f, float radius = 1f, int start_angle = 0, int end_angle = 360)
		
		public ParticleComponent(World world, double velocityMagnitude,int MaxParticles = 1024, float gravity = 1f,float _startScale = 1f, float _deltaScale = 0f)
			: base()
		{
			HandledEvents = EventType.OnUpdate;
			actor = new Actor();
			actor.AddComponent(new Body());

			//Particle Controller Properties
            controller = new ParticleController(new Vector2d(actor.Body.Position.X, actor.Body.Position.Y), velocityMagnitude, gravity, _startScale, _deltaScale);
            particle_group = new ParticleGroup(MaxParticles);

			world.AddActor(actor);
		}

		public void setupEmitter(ParticleEmitter emitter,bool doFadeWithTime=false)
		{
           /* switch (emitter)
            {
				case(EmitterType.Circle):
					emitter = new CircleEmitter(color, velocityMagnitude, emitDelay, MaxParticles, maxLifeTime, radius, start_angle, end_angle);
					break;
				case (EmitterType.Line):
					emitter = new LineEmitter(color, velocityMagnitude, emitDelay, MaxParticles, maxLifeTime, radius, start_angle);
					break;
			}*/
            this.emitter = emitter;
			Texture texture = Resources.TheResources.GetTexture("dot");
            ParticleRenderState renderstate = new ParticleRenderState(texture, particle_group.Particles, new Vector2(0.1f, 0.1f));
			actor.AddComponent(renderstate);
            controller.fadeWithTime = doFadeWithTime;
		}

		public override void OnUpdate(object sender, FrameEventArgs e)
		{
			var dt = e.Time;
			emitter.Update(dt,ref particle_group);
            ((ParticleRenderState)actor.RenderState).particleList = particle_group.Particles;
            controller.Update(dt, ref particle_group);
		}

	}
}

