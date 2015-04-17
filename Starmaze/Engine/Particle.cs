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


        public Particle(Vector2d pos, double magnitude, Color4 color, Vector2d angle, double life=5f, float s=1f)
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
        private float startScale = 1.0f; //add scaling over time begining at start scale
        private float deltaScale = 0.0f; //add scaling over time by delta scale
        private float gravity=1.0f;

        public ParticleController()
        {
            position = Vector2d.Zero;
            startScale = 1f;
            deltaScale = 0f;
        }

        public ParticleController(Vector2d position, double velocity_Magnitude,float gravity = 1.0f, float startScale = 1f, float deltaScale = 0f)
		{
            this.position = position;
            this.startScale = startScale;
            this.deltaScale = deltaScale;
            this.gravity = gravity;
		}

		public void Update(double dt,ref List<Particle> list)
		{
			// OPT: This could prolly be more efficient.
			// But a core i5 handles 50k particles without much sweat, so, no sweat.
            Random rand = new Random();

            for (int i = list.Count-1; i > -1 ; i--)
            {
                var p = list[i];
                if (p.Life <= 0)
                {
                    list.RemoveAt(i);
                    continue;
                }
                //***Calculate the factors that will affect velocty***
                //1. Applying gravity
                var _gravity =-1*Vector2d.UnitY*(gravity);
                p.velocity += _gravity * dt;
                //*(velocity * rand.NextDouble() * dt);
                var speed = p.velocity*dt;
                var pos = Vector2d.Zero;
               //Vector2d.Multiply(ref vel, ref _gravity, out vel);
              
                //Final Step - add the calculated Velocity the particle's position
                Vector2d.Add(ref p.Position, ref speed, out pos);
                p.Position = pos;
                p.Life -= dt;
                list[i] = p;
			}
           // Log.Message(String.Format("Particle ({0}) V {0}", list[0].Position, list[0].Velocity));
		
		}
	}

    public enum EmitterType : int
    {
        Cone, Circle, Square, Rectangle
    }

    public struct EmitterShape
    {
        public float radius, length, width;
        public int start_angle, end_angle;

        public EmitterShape(float radius = 1f, int start_angle = 0, int end_angle = 359, float length = 1f, float width = 1f)
        {
            this.radius = radius;
            this.length = length;
            this.width = width;
            this.start_angle = start_angle;
            this.end_angle = end_angle;
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
        protected EmitterShape emitter_shape;
        public List<Particle> Particles;
        protected Color4 color;

        protected abstract void AddParticle(Vector2d pos, double velocityMagnitude, Color4 color, Vector2d angle, double age = 1.0);

        public abstract void Update(double dt);

    }

    /// <summary>
    /// 
    /// </summary>
    public class CircleEmitter : ParticleEmitter
    {
        public float radius;
        public int start_angle, end_angle,current_angle;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <param name="velocityMagnitude"></param>
        /// <param name="emitDelay"></param>
        /// <param name="MaxParticles"></param>
        /// <param name="maxLifeTime"></param>
        public CircleEmitter(Color4 color, double velocityMagnitude = 1f, double emitDelay = 0.1, int MaxParticles = 1024, double maxLifeTime = 1f,float radius=1f, int start_angle=0,int end_angle=360)
        {
            //Particle Emitter Properties
            rand = new Random();
            this.color = color;
            this.velocityMagnitude = velocityMagnitude;
            this.emitDelay = emitDelay;
            this.maxLifeTime = maxLifeTime;
            this.radius = radius;
            this.start_angle = start_angle;
            this.end_angle = end_angle;
            Particles = new List<Particle>(MaxParticles); 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        public override void Update(double dt)
        {
            Vector2d position = Vector2d.Zero, angleVec = Vector2d.Zero;
            Random random = new Random();
            double angle;

            lastTime += dt;
            while (lastTime >= nextTime)
            {
                nextTime += emitDelay;

                angle = random.Next(start_angle, end_angle);
                position = new Vector2d(radius * Math.Cos(angle), radius * Math.Sin(angle));
                Vector2d.Normalize(ref position, out angleVec);
                AddParticle(position, velocityMagnitude, color, angleVec, maxLifeTime);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="velocityMagnitude"></param>
        /// <param name="color"></param>
        /// <param name="angle"></param>
        /// <param name="age"></param>
        protected override void AddParticle(Vector2d pos, double velocityMagnitude, Color4 color, Vector2d angle, double age = 1.0)
        {
            Particles.Add(new Particle(pos, velocityMagnitude, color, angle, age));
            //Log.Message(String.Format("Particle ({0}) V {0}", pos, vel));
        }
    }


	/// <summary>
	/// An object that draws a ParticleGroup.
	/// </summary>
	
	/// <summary>
	/// A test component that emits particles.
	/// </summary>
	// XXX: Doesn't draw anything yet...  How exactly to integrate the ParticleRenderer
	// with the Renderer pipeline is something I need to think about.
	class ParticleComponent : Component
	{
        Actor actor;
        ParticleController controller;
        public double maxLifeTime = 1.0f;
        public float startScale = 1.0f;
        public float deltaScale = 0.0f;
        public double velocityMagnitude;
        
        ParticleEmitter emitter;
        EmitterType emitter_type;

        public ParticleComponent(Actor owner, World world,double velocityMagnitude,Color4 color, double emitDelay = 0.1,
            double maxLifeTime = 3f, int MaxParticles = 1024, float _startScale = 1f, float _deltaScale = 0f, float gravity = 1f)
            : base(owner)
		{
			HandledEvents = EventType.OnUpdate;
            actor = new Actor();
            actor.Body = new Body(actor);

            //Particle Controller Properties
            this.velocityMagnitude = velocityMagnitude;
            this.maxLifeTime = maxLifeTime;
            this.startScale = _startScale;
            this.deltaScale = _deltaScale;

            emitter = new CircleEmitter(color, velocityMagnitude, emitDelay, MaxParticles,maxLifeTime);
            controller = new ParticleController(owner.Body.Position, maxLifeTime, gravity, startScale, deltaScale);

            Texture texture = Resources.TheResources.GetTexture("dot");
            ParticleRenderState renderstate = new ParticleRenderState(actor, texture, color,emitter.Particles , new Vector2(0.1f, 0.1f));
            actor.RenderState = renderstate;
            world.AddActor(actor);            
		}

		public override void OnUpdate(object sender, FrameEventArgs e)
		{
            var dt = e.Time;

            /*switch (emitter_type)
            {
                case EmitterType.Circle:*/
                   /* break;
            }*/

            emitter.Update(dt) ;
            ((ParticleRenderState)actor.RenderState).particleList = emitter.Particles;
            controller.Update(dt, ref emitter.Particles);
		}

	}
}
