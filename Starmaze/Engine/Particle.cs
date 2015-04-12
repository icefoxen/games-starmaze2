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

        public Vector2d Velocity
        {
            get
            {
                return Angle * velocityMagnitude;
            }
        }

        public Particle(Vector2d pos, double magnitude, Color4 color, Vector2d angle, double life=5f, float s=1f)
		{
			Position = pos;
            velocityMagnitude = magnitude;
			Color = color;
            Angle = angle;
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
        private Vector2d position;
        private double maxLifeTime = 1.0f;
        private float startScale = 1.0f;
        private float deltaScale = 0.0f;
        private double velocity_Magnitude;
        private float gravity=1.0f;

        public ParticleController()
        {
            position = Vector2d.Zero;
            maxLifeTime = 1f;
            startScale = 1f;
            deltaScale = 0f;
            velocity_Magnitude = 5;
        }

        public ParticleController(Vector2d position, double velocity_Magnitude,
            double maxLifeTime = 1f, float gravity = 1.0f, float startScale = 1f, float deltaScale = 0f)
		{
            this.position = position;
            this.velocity_Magnitude = velocity_Magnitude;
            this.maxLifeTime = maxLifeTime;
            this.startScale = startScale;
            this.deltaScale = deltaScale;
            this.gravity = gravity;
		}

		public void Update(double dt,ref List<Particle> list)
		{
			// OPT: This could prolly be more efficient.
			// But a core i5 handles 50k particles without much sweat, so, no sweat.
            Random rand = new Random();

            for (int i = list.Count-1; i > 0 ; i--)
            {
                var p = list[i];
                if (p.Life <= 0)
                {
                    list.RemoveAt(i);
                    continue;
                }

                //Calculate the factors that will affect velocty

                //1. Applying gravity
                var _gravity =-1*Vector2d.UnitY*(gravity);
                _gravity *= (p.Life/p.MaxLife);
                //*(velocity * rand.NextDouble() * dt);
                var vel = p.Velocity*dt;
                var pos = position;
               // Vector2d.Multiply(ref vel, ref _gravity, out vel);
              
                //Final Step - add the calculated Velocity the particle's position
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
	/// A test component that emits particles.
	/// </summary>
	// XXX: Doesn't draw anything yet...  How exactly to integrate the ParticleRenderer
	// with the Renderer pipeline is something I need to think about.
	class ParticleComponent : Component
	{

        Actor actor;
        ParticleController controller;
        public Vector2d position;
        public double maxLifeTime = 1.0f;
        public float startScale = 1.0f;
        public float deltaScale = 0.0f;
        public double velocityMagnitude;
        
        //Emitter Properties
        double lastTime = 0.0;
        double nextTime = 0.0;
        Random rand;
        double emitDelay;
        EmitShape emitter_shape;
        List<Particle> Particles;
        Color4 color;

        public enum EmitType : int
        {
            Cone, Circle, Square, Rectangle
        }

        public struct EmitShape
        {
            public EmitType type;
            public float radius, length, width;
            public int start_angle, end_angle;
            public EmitShape(EmitType type = EmitType.Circle, float radius = 1f, float length = 1f, float width = 1f)
            {
                this.type = type;
                this.radius = radius;
                this.length = length;
                this.width = width;
                this.start_angle = 0;
                this.end_angle = 360;
            }
        }

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

            this.color = color;

            //Particle Emitter Properties
            rand = new Random();
            this.emitDelay = emitDelay;
            Particles = new List<Particle>(MaxParticles);
            emitter_shape = new EmitShape(EmitType.Circle, 20f);

            controller = new ParticleController(owner.Body.Position, velocityMagnitude, maxLifeTime, gravity, startScale, deltaScale);

            Texture texture = Resources.TheResources.GetTexture("dot");
            ParticleRenderState renderstate = new ParticleRenderState(actor, texture, color, Particles,new Vector2(0.1f, 0.1f));
            actor.RenderState = renderstate;
            world.AddActor(actor);            
		}

		public override void OnUpdate(object sender, FrameEventArgs e)
		{
            var dt = e.Time;
            //ParticleEmitter emitter = ((ParticleRenderState)actor.RenderState).emitter;
            Update(dt);
            ((ParticleRenderState)actor.RenderState).particleList = Particles;
            controller.Update(dt, ref Particles);
		}

		public void AddParticle(Vector2d pos, double velocityMagnitude, Color4 color, Vector2d angle, double age = 1.0)
		{
            Particles.Add(new Particle(pos, velocityMagnitude, color, angle, age));
            //Log.Message(String.Format("Particle ({0}) V {0}", pos, vel));
		}		

        private void EmitByShape()
        {
            Vector2d position= Vector2d.Zero,angleVec;
            Random random = new Random();
            double angle;
            switch(emitter_shape.type){
                case(EmitType.Circle):
                case (EmitType.Cone):
                     angle = random.Next(emitter_shape.start_angle, emitter_shape.end_angle);
                     position = new Vector2d(emitter_shape.radius * Math.Cos(angle), emitter_shape.radius * Math.Sin(angle));
                    break;
            }
            Vector2d.Normalize(ref position, out angleVec);
            AddParticle(position, velocityMagnitude, color, angleVec, maxLifeTime);
        }

        public void Update(double dt)
        {
            lastTime += dt;
            while (lastTime >= nextTime)
            {
                nextTime += emitDelay;
                /*var xOffset = rand.NextDouble() * 0.2;
                var yOffset = rand.NextDouble() * 0.2;*/
                EmitByShape();
                //AddParticle(Vector2d.Zero, 0.0,color, new Vector2d(xOffset, yOffset), lifetime);
            }
        }

	}
}
