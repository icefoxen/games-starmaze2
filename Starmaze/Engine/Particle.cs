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
        public int cfIndex;
        public double timeAlive
        {
            get { return MaxLife - Life; }
        }

		public Particle(Vector2d pos, double magnitude, Color4 color, Vector2d angle, double life = 5f, float s = 0.1f)
		{
			Position = pos;
			velocityMagnitude = magnitude;
			Color = color;
			Angle = angle;
			MaxLife = life;
			Life = life;
			scale = s;
			velocity = Angle * velocityMagnitude;
            cfIndex=0;
		}
	}


	/// <summary>
	/// Updates the motion and state for a  List of Particles. Handles the Logic
	/// </summary>
	public class ParticleController
	{
		private Vector2d position;
        public Vector2d Position
        {
            get { return position; }
            set { position = value; }
        }
		private float startScale = 1.0f;
		//add scaling over time begining at start scale
		private float deltaScale = 0.0f;
		//add scaling over time by delta scale
		private float gravity = 1.0f;
        public bool fadeWithTime = false;
        public bool scaleWithTime = false;
        public bool changeColorWithTime = false;

		public ParticleController()
		{
			startScale = 1f;
			deltaScale = 0f;
		}

		public ParticleController(Vector2d position, double velocity_Magnitude, float gravity = 1.0f,float deltaScale = 0f)
		{
			this.gravity = gravity;
            this.deltaScale = deltaScale;
		}

        // OPT: This could prolly be more efficient.
        // But a core i5 handles 50k particles without much sweat, so, no sweat.
		public void Update(double dt, ref ParticleGroup group)
		{			
            List<Particle> list = group.Particles;
            Color4 newColor;
            float fadeRate,colorRate = 0.8f * (float)dt;
            Color4  nextColor = Color4.White;

            for (int i = list.Count - 1; i > -1; i--)
             {
                    var p = list[i];

                    if (scaleWithTime)
                    {
                        p.scale += (float)(deltaScale * dt);
                        p.scale = (float)SMath.Clamp(p.scale, 0.1, 5);
                    }

                    if (fadeWithTime)
                    {
                        fadeRate = (float)(dt / p.MaxLife);
                        newColor = new Color4(p.Color.R - fadeRate, p.Color.G - fadeRate, p.Color.B - fadeRate, p.Color.A);
                        //newColor = SMath.Lerp(p.Color, Color4.Black, 0.4f);
                        p.Color = newColor;
                    }
                    if (changeColorWithTime)
                      {
                          group.colorFader.setColor(ref p, ref nextColor, ref colorRate);
                          colorRate *= (float)dt;

                          p.Color.R -= (p.Color.R - nextColor.R) * colorRate;
                          p.Color.G -= (p.Color.G - nextColor.G) * colorRate;
                          p.Color.B -= (p.Color.B - nextColor.B) * colorRate;
                          /*p.Color = new Color4(p.Color.R - (p.Color.R - nextColor.R) * colorRate,
                               p.Color.G - (p.Color.G - nextColor.G) * colorRate,
                               p.Color.B - (p.Color.B - nextColor.B) * colorRate, p.Color.A);

                          float red = startColor.R - (endColor.R - startColor.R) * colorRate;
                          float green = startColor.G - (endColor.G - startColor.G) * colorRate;
                          float blue = startColor.B - (endColor.B - startColor.B) * colorRate;                            
                         p.Color = new Color4(red, green, blue, 1);*/
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

                    if (p.Life < 0)
                        list = group.Remove(i);
            }
			// Log.Message(String.Format("Particle ({0}) V {0}", list[0].Position, list[0].Velocity));

		}

	}

    /// <summary>
    /// ColorFader manages a sorted list of ColorFades . When a particles life/maxlife is > a threshold,
    /// the particle uses the next set of ColorFades
    /// </summary>
    public class ColorFader
    {

        /// <summary>
        /// ColorFade holds a Color and a threshold
        /// </summary>
        struct ColorFade
        {
            public double threshold;
            public Color4 color;
        }
        
        //The List of color fades
        List<ColorFade> list;
 
        /// <summary>
        /// Makes a ColorFader, Must have the 1st ColorFader's threshold set to 0.
        /// The order the Dictionary entries will be the order of the colors for the Fader (at least for now)
        /// </summary>
        /// <param name="dictionary"> double (the key and the threshold) is about how many seconds for the particle to completely become that color
        /// , Color4 is the color for that </param>
        public ColorFader(Dictionary<double,Color4> dictionary)
        {
            ColorFade cf;
            //giving it a max of 5, don't think we should need more than that
            list = new List<ColorFade>(5);
            foreach (var item in dictionary)
            {
                cf.color = item.Value;
                cf.threshold = item.Key;
                list.Add(cf);
            }
        }
 
        /// <summary>
        ///Sets the Next Color for the particle to fade to. It also updates the particles Color Fader index
        ///and sets the rate to change between colors
        /// </summary>
        /// <param name="particle"></param>
        public void setColor(ref Particle particle,ref Color4 nextcolor,ref float colorRate)
        {
            if (particle.cfIndex + 1 < list.Count && particle.timeAlive >= list[particle.cfIndex].threshold)
            {
                particle.cfIndex += 1;
            }

            //colorRate = (float)((list[particle.cfIndex].threshold - particle.timeAlive) / list[particle.cfIndex].threshold);
            colorRate = (float)(particle.MaxLife / (list[particle.cfIndex].threshold - list[particle.cfIndex-1].threshold));
            nextcolor = list[particle.cfIndex].color;
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
		protected double velocityMagnitude, maxLifeTime;
        public double emitDelay;
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
		/// <param name="maxLifeTime"></param>
		public CircleEmitter(Color4 color, float radius = 5f, int start_angle = 0, int end_angle = 360, double velocityMagnitude = 3f, double emitDelay = 0.1,double maxLifeTime = 3f)
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
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dt"></param>
        public override void Update(double dt, ref ParticleGroup particle_group)
		{
			Vector2d position = Vector2d.Zero, angleVec = Vector2d.Zero;
            float rand_radius = radius * (float)rand.NextDouble();
			//current_angle = start_angle * Math.PI / 180;

			lastTime += dt;
			//for (int i = 0; i <= 360 && lastTime >= nextTime; i += 10, current_angle++) {			//while (lastTime >= nextTime)

            current_angle++;
			if (current_angle > end_angle - 1) {
				current_angle = start_angle;
				nextTime += emitDelay;
				//break;
			}

			//angle = random.Next(start_angle, end_angle);
            position = new Vector2d(rand_radius * Math.Cos(current_angle * Math.PI / 180), rand_radius * Math.Sin(current_angle * Math.PI / 180));
			Vector2d.Normalize(ref position, out angleVec);
			//Log.Message(String.Format("Particle Angle {0} , {1} , {2}", current_angle, position.X, position.Y));
            particle_group.AddParticle(position, velocityMagnitude, color, angleVec, maxLifeTime);
		//}
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
        int direction = 1;
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
        public PointEmitter(Color4 color, double xVelocity = 3f, double yVelocity = 3f, double emitDelay = 0.1, double maxLifeTime = 3f)
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
        /// <param name="particle_group"></param>
        public override void Update(double dt, ref ParticleGroup particle_group)
        {
            double xV = rand.NextDouble() * velocity.X*direction;
            direction *= -1;
            //double yV = rand.NextDouble() * velocity.Y;
            
            //Will Add Code Soon
            Vector2d position = Vector2d.Zero, angleVec = new Vector2d(xV, velocity.Y);
         
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
    /// An object that draws a ParticleGroup.
    /// </summary>
    public class ParticleGroup
    {

        public List<Particle> Particles;
        /*ColorFader is in ParticleGroup so that it's accessible in ParticleRenderer's ParticleGroup
         *and the Particle Group sent to Particle Controllers update 
         */ 
        public ColorFader colorFader;
        public Body Body;
        
        public ParticleGroup(int MaxParticles)
        {
            Particles = new List<Particle>(MaxParticles);
        }

        public void AddParticle(Vector2d pos, double velocityMagnitude, Color4 color, Vector2d angle, double age = 1.0)
        {
            Util.ConvertVector2(Body.PBody.Position);
            
            Particles.Add(new Particle(new Vector2d(pos.X + Body.Position.X, pos.Y + Body.Position.Y), velocityMagnitude, color, angle, age));
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
	/// A component that emits particles.
	/// </summary>
	// XXX: Exactly to integrate the ParticleRenderer with the Renderer pipeline is something I need to think about.
	class ParticleComponent : Component
	{
		ParticleController controller;
		ParticleEmitter emitter;
        ParticleGroup particle_group;
        public double velocityMagnitude;
        public int maxParticles;
        public float gravity,deltaScale;
        public ColorFader colorFader = null;
        public bool doFadeWithTime = false, scaleWithTime = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_velocityMagnitude"></param>
        /// <param name="MaxParticles"></param>
        /// <param name="_gravity"></param>
        /// <param name="_deltaScale"></param>
        public ParticleComponent(double _velocityMagnitude, int MaxParticles = 1024, float _gravity = 1f, float _deltaScale = 0f)
            : base()
        {
            HandledEvents = EventType.OnUpdate;
            //Setting these for Serialization purposes
            this.velocityMagnitude = _velocityMagnitude;
            this.maxParticles = MaxParticles;
            this.gravity = _gravity;
            this.deltaScale = _deltaScale;
            //Particle Controller Properties
            controller = new ParticleController(Vector2d.One, _velocityMagnitude, _gravity, _deltaScale);
            particle_group = new ParticleGroup(MaxParticles);

            Texture texture = Resources.TheResources.GetTexture("dot");
            this.RenderState = new ParticleRenderState(texture, particle_group.Particles);
        }

        /// <summary>
        /// Sets up a Particle Emitter. 
        /// Can set for particles to fade into the background with time, to fade from multiple colors, and scale with time
        /// </summary>
        /// <param name="_emitter"></param>
        /// <param name="_doFadeWithTime"></param>
        /// <param name="_colorFader"></param>
        /// <param name="_scaleWithTime"></param>
        public void setupEmitter(ParticleEmitter _emitter, bool _doFadeWithTime = false, ColorFader _colorFader = null, bool _scaleWithTime = false)
        {
            this.emitter = _emitter;
           /* Texture texture = Resources.TheResources.GetTexture("dot");
            this.RenderState = new ParticleRenderState(texture, particle_group.Particles);
            */
            //AddComponent(this.RenderState);
            particle_group.Body = Owner.Body;
            //Particle Controlling options
            controller.fadeWithTime = _doFadeWithTime;
            particle_group.colorFader = _colorFader;
            controller.changeColorWithTime = (_colorFader != null);
            controller.scaleWithTime = _scaleWithTime;

            //setting these for Serialization purposes
            this.doFadeWithTime = _doFadeWithTime;
            this.colorFader = _colorFader;
            this.scaleWithTime = _scaleWithTime;
        }

        /// <summary>
        /// Updates the particle emitter, the 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		public override void OnUpdate(object sender, FrameEventArgs e)
		{
			var dt = e.Time;
			emitter.Update(dt,ref particle_group);
            ((ParticleRenderState)this.RenderState).particleList = particle_group.Particles;
            controller.Update(dt, ref particle_group);
		}

	}
}

