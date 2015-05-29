using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

namespace Starmaze.Engine
{
	/// <summary>
	/// A particle!  Just a struct to contain particle properties.
	/// </summary>
	public struct Particle
	{
		//Life times are doubles
		//scales are
		public Vector2d Position;
		public float Rotation;
		public float Scale;
		public Color4 Color;
		public double Life;
		public readonly double MaxLife;
		public Vector2d Velocity;
		public int ColorFadeIndex;

		public double timeAlive {
			get { return MaxLife - Life; }
		}

		public Particle(Vector2d position, float rotation = 0, float scale = 0.1f, Vector2d velocity = default(Vector2d), Color4 color = default(Color4), double life = 5f)
		{
			Position = position;
			Rotation = rotation;
			Scale = scale;
			Color = color;
			MaxLife = life;
			Life = life;
			Velocity = velocity;
			ColorFadeIndex = 0;
		}

		public Particle(Vector2d position, float rotation = 0, float scale = 0.1f, double velocityAngle = 0, double velocityMagnitude = 0, Color4 color = default(Color4), double life = 5f) :
			this(position, rotation, scale, new Vector2d(Math.Cos(velocityAngle), Math.Sin(velocityAngle) * velocityMagnitude), color, life)
		{
		}
	}


	/// <summary>
	/// Updates the motion and state changes for a ParticleGroup.
	/// Also handles emission.
	/// </summary>
	public class ParticleController
	{
		public Vector2d Position { get;	set; }

		float deltaScale = 0.0f;
		//add scaling over time by delta scale
		float gravity = 1.0f;
		public bool scaleWithTime = false;
		public bool changeColorWithTime = false;

		public ParticleController()
		{
		}

		public ParticleController(Vector2d position, double velocity_Magnitude, float gravity = 1.0f, float deltaScale = 0f)
		{
			this.gravity = gravity;
			this.deltaScale = deltaScale;
		}

		// OPT: This could prolly be more efficient.
		// But a core i5 handles 50k particles without much sweat, so, no sweat.
		public void Update(double dt, ParticleGroup group)
		{
			var fdt = (float)dt;
			var particles = group.Particles;
			// We iterate downwards through the list so if we remove a particle we don't
			// reorder anything we haven't already updated.
			for (int i = particles.Count - 1; i > -1; i--) {
				var p = particles[i];

				if (scaleWithTime) {
					p.Scale += (deltaScale * fdt);
					p.Scale = (float)SMath.Clamp(p.Scale, 0.1, 5);
				}
				if (changeColorWithTime) {
                    doColorFade(ref p, group.colorFader,fdt);
                    //group.colorFader.setColor(ref p);
				}

                doMovement(ref p, dt);
                particles[i] = p;
				if (p.Life < 0) {
					group.Remove(i);
				}
			}         
			
		}
        
        /// <summary>
        /// Does Color Fading on the particle
        /// </summary>
        /// <param name="p"></param>
        /// <param name="colorFader"></param>
        /// <param name="fdt"></param>
        void doColorFade(ref Particle p, ColorFader colorFader, float fdt)
        {
            colorFader.updateColorFadeAndRate(ref p);
            /*p.Color.R -= (p.Color.R - colorFader.nextColor.R) * colorFader.ColorRate * fdt;
            p.Color.G -= (p.Color.G- colorFader.nextColor.G) * colorFader.ColorRate * fdt;
            p.Color.B -= (p.Color.B - colorFader.nextColor.B) * colorFader.ColorRate * fdt;
            p.Color.A -= (p.Color.A - colorFader.nextColor.A) * colorFader.ColorRate * fdt;*/
            //colorRate = colorFader.ColorRate*fdt;
            p.Color.R += (colorFader.nextColor.R - p.Color.R) * colorFader.ColorRate * fdt;
            p.Color.G += (colorFader.nextColor.G - p.Color.G) * colorFader.ColorRate * fdt;
            p.Color.B += (colorFader.nextColor.B - p.Color.B) * colorFader.ColorRate * fdt;
            p.Color.A += (colorFader.nextColor.A - p.Color.A) * colorFader.ColorRate * fdt;
        }

        void doMovement(ref Particle p,double dt)
        {
            //***Calculate the factors that will affect velocty***
            //1. Applying gravity
            var _gravity = -1 * Vector2d.UnitY * (gravity);
            p.Velocity += _gravity * dt;
            //*(velocity * rand.NextDouble() * dt);
            var speed = p.Velocity * dt;
            var pos = Vector2d.Zero;
            //Vector2d.Multiply(ref vel, ref _gravity, out vel);

            //Final Step - add the calculated Velocity the particle's position
            Vector2d.Add(ref p.Position, ref speed, out pos);
            p.Position = pos;
            p.Life -= dt;
        }
	}

	/// <summary>
	/// ColorFader manages a sorted list of ColorFades . When a particles life/maxlife is > a given time,
	/// the particle uses the next set of ColorFades
	/// </summary>
	public class ColorFader
	{

		/// <summary>
		/// ColorFade holds a Color4 and a fade time
		/// </summary>
		public struct ColorFade
		{
            public double FadeTime {get; set; }
			public Color4 Color{get;set;}
		}
        
		//The List of color fades
		public List<ColorFade> FadeList {get;set;}
        public float ColorRate;
        public Color4 nextColor = Color4.White;


        public ColorFader()
        {
        }

		/// <summary>
		/// Makes a ColorFader, Must have the 1st ColorFader's threshold set to 0.
		/// The order the Dictionary entries will be the order of the colors for the Fader (at least for now)
		/// </summary>
		/// <param name="dictionary"> double (the key and the threshold) is about how many seconds for the particle to completely become that color
		/// , Color4 is the color for that </param>
		public ColorFader(Dictionary<double,Color4> dictionary)
		{
			ColorFade cf = new ColorFade();
			//giving it a starting capacity of 4, don't think we should need more than that
			FadeList = new List<ColorFade>(4);
			foreach (var item in dictionary) {
				cf.Color = item.Value;
				cf.FadeTime = item.Key;
				FadeList.Add(cf);
			}
		}

		public ColorFader(List<ColorFade> colorFades)
		{
			FadeList = colorFades;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="time"></param>
        /// <param name="Color"></param>
        public void insertColorFader(int index, double time, Color4 Color)
        {
            ColorFade newFade=new ColorFade();
            newFade.FadeTime=time;
            newFade.Color = Color;
            FadeList.Insert(index,newFade);
        }
		/// <summary>
		///Sets the Next Color for the particle to fade to. It also updates the particles Color Fader index
		///and sets the rate to change between colors
		/// </summary>
		/// <param name="particle"></param>
		public void updateColorFadeAndRate(ref Particle particle)
		{
			if (particle.ColorFadeIndex + 1 < FadeList.Count && particle.timeAlive >= FadeList[particle.ColorFadeIndex].FadeTime) {
				particle.ColorFadeIndex += 1;
			}
			var lastFader = FadeList[particle.ColorFadeIndex - 1];
			var fader = FadeList[particle.ColorFadeIndex];
            nextColor = fader.Color;
            ColorRate = (float)(particle.timeAlive / (fader.FadeTime - lastFader.FadeTime));  
		}

		/// <summary>
		/// Returns a Dictionary<double,Color4> of all the colors with their threshold
		/// </summary>
		/// <returns>A dictionary of the Colors and their thresholds </returns>
		public Dictionary<double,Color4> getColorFaders()
		{
			var dictionary = new Dictionary<double, Color4>();
            
			foreach (ColorFade colorfade in FadeList) {
				dictionary.Add(colorfade.FadeTime, colorfade.Color);
			}
			return dictionary;
		}
	}

	/// <summary>
	/// The Base class for managing how the particles are first emitted and where they are emitted.
	/// In particular, it also determines what properties a particle has when it is first created.
	/// </summary>
	abstract public class ParticleEmitter
	{
		//Emitter Properties
		protected double lastTime = 0.0;
		protected double nextTime = 0.0;
		protected Random rand;
		public double velocityMagnitude{get;set;}
        public double maxLifeTime{get;set;}
        public double emitDelay {get;set;}
        public Color4 Color {get;set;}
        public ParticleEmitter(Color4 Color,double velocityMagnitude = 3f, double emitDelay = 0.1, double maxLifeTime = 3f)
        {
            this.Color = Color;
            this.velocityMagnitude = velocityMagnitude;
            this.emitDelay = emitDelay;
            this.maxLifeTime = maxLifeTime;
            rand = new Random();
        }
		public abstract void Update(double dt, ParticleGroup particle_group);

	}

	/// <summary>
	/// 
	/// </summary>
	public class CircleEmitter : ParticleEmitter
	{
		public float radius {get;set;}
		public int start_angle {get;set;}
        public int end_angle {get;set;}
        int current_angle;

		public CircleEmitter(Color4 Color, float radius = 5f, int start_angle = 0, int end_angle = 360, double velocityMagnitude = 3f, double emitDelay = 0.1, double maxLifeTime = 3f)
		:base(Color,velocityMagnitude,emitDelay,maxLifeTime)
        {
			//Particle Emitter Properties
			this.radius = radius;
			this.start_angle = start_angle;
			current_angle = start_angle;
			this.end_angle = end_angle;
		}

		public override void Update(double dt, ParticleGroup particle_group)
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
            particle_group.AddParticle(position, velocityMagnitude * angleVec, Color, maxLifeTime);
			//}
		}

	}

	public class LineEmitter : ParticleEmitter
	{
        public float length { get; set; }
        public int angle { get; set; }

		public LineEmitter(Color4 Color, float length = 1f, int angle = 0, double velocityMagnitude = 3f, double emitDelay = 0.1, double maxLifeTime = 1f)
            : base(Color, velocityMagnitude, emitDelay, maxLifeTime)
        {
			//Particle Emitter Properties			
			this.length = length;
			this.angle = angle;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dt"></param>
		public override void Update(double dt, ParticleGroup particle_group)
		{
			Vector2d position = Vector2d.Zero, angleVec = Vector2d.Zero;

			//current_angle = start_angle * Math.PI / 180;

			lastTime += dt;
			//for (int i = 0; i <= 360 && lastTime >= nextTime; i += 10, current_angle++)
			while (lastTime >= nextTime) {
				nextTime += emitDelay; 
				position = new Vector2d(Math.Cos(angle * Math.PI / 180), Math.Sin(angle * Math.PI / 180));
				Vector2d.Normalize(ref position, out angleVec);
				//Log.Message(String.Format("Particle Angle {0} , {1} , {2}", current_angle, position.X, position.Y));
                particle_group.AddParticle(position, velocityMagnitude * angleVec, Color, maxLifeTime);
			}
		}

	}

	public class PointEmitter : ParticleEmitter
	{
        public Vector2d range { get; set; }
		int direction = 1;

        public PointEmitter()
            : base(Color4.White)
        {
        }

        public PointEmitter(Color4 Color, Vector2d range,double velocityMagnitude=3f, double emitDelay = 0.1, double maxLifeTime = 3f)
            : base(Color, velocityMagnitude, emitDelay, maxLifeTime)
        {
			//Particle Emitter Properties
            this.range = range;
		}

		public override void Update(double dt, ParticleGroup particle_group)
		{
            double xV = rand.NextDouble() * range.X*direction;
			direction *= -1;
			//double yV = rand.NextDouble() * velocity.Y;
            
			//Will Add Code Soon
            Vector2d position = Vector2d.Zero, angleVec = new Vector2d(xV, range.Y);
         
			//current_angle = start_angle * Math.PI / 180;

			lastTime += dt;
			//for (int i = 0; i <= 360 && lastTime >= nextTime; i += 10, current_angle++)
			while (lastTime >= nextTime) {
				nextTime += emitDelay;
				//position = new Vector2d(Math.Cos(angle * Math.PI / 180), Math.Sin(angle * Math.PI / 180));
				Vector2d.Normalize(ref angleVec, out angleVec);
                particle_group.AddParticle(position, velocityMagnitude*angleVec, Color, maxLifeTime);
                
			}
		}

	}

	/// <summary>
	/// An object that contains a bunch of particles.
	/// </summary>
	public class ParticleGroup
	{

		public List<Particle> Particles;
		/*ColorFader is in ParticleGroup so that it's accessible in ParticleRenderer's ParticleGroup
         *and the Particle Group sent to Particle Controllers update 
         */
		public ColorFader colorFader;
		public Vector2d Position = Vector2d.Zero;

		public ParticleGroup(int MaxParticles)
		{
			Particles = new List<Particle>(MaxParticles);
		}

		public void Add(Particle p)
		{
			Particles.Add(p);
		}

		public void AddParticle(Vector2d pos, Vector2d velocity, Color4 color, double life = 1.0)
		{
            Particles.Add(new Particle(pos + Position, velocity: velocity, color: color, life: life));
			//Log.Message(String.Format("Particle ({0}) V {0}", pos, vel));
		}

		/// <summary>
		/// Removes a particle 
		/// </summary>
		/// <param name="i">the index of the particle to remove</param>
		public void Remove(int i)
		{
			// RemoveAt() shifts everything after the removed particle down a notch, which can be slow.
			// So we replace the removed particle with the last particle in the list, and remove the last
			// particle.
			Particles[i] = Particles[Particles.Count - 1];
			Particles.RemoveAt(Particles.Count - 1);
		}

	}

   

	/// <summary>
	/// A component that emits particles.
	/// </summary>
	// XXX: Exactly how to integrate the ParticleRenderer with the Renderer pipeline is something I need to think about.
	class ParticleComponent : Component
	{
		ParticleController controller;
		public ParticleEmitter emitter;
		ParticleGroup particleGroup;
        public double velocityMagnitude {get;set;}
        public int MaxParticles { get; set; }
        public float gravity{get;set;}
        public float deltaScale{get;set;}
        public ColorFader ColorFader
        {
            get { return particleGroup.colorFader; }
        }
        public bool doFadeWithTime {get;set;}
        public bool scaleWithTime { get; set; }

		public ParticleComponent(double _velocityMagnitude, int MaxParticles = 1024, float _gravity = 1f, float _deltaScale = 0f)
			: base()
		{
			HandledEvents = EventType.OnUpdate;
			//Setting these for Serialization purposes
			//this.velocityMagnitude = _velocityMagnitude;
			this.MaxParticles = MaxParticles;
			//Particle Controller Properties
			controller = new ParticleController(Vector2d.One, _velocityMagnitude, _gravity, _deltaScale);
			particleGroup = new ParticleGroup(MaxParticles);

			Texture texture = Resources.TheResources.GetTexture("dot");
			RenderState = new ParticleRenderState(texture, particleGroup);
		}

		/// <summary>
		/// Sets up a Particle Emitter. 
		/// Can set for particles to fade into the background with time, to fade from multiple colors, and scale with time
		/// </summary>
		/// <param name="_emitter"></param>
		/// <param name="_doFadeWithTime"></param>
		/// <param name="_colorFader"></param>
		/// <param name="_scaleWithTime"></param>
		public void setupEmitter(ParticleEmitter _emitter, ColorFader _colorFader = null, bool _scaleWithTime = false)
		{
			this.emitter = _emitter;
			/* Texture texture = Resources.TheResources.GetTexture("dot");
            this.RenderState = new ParticleRenderState(texture, particle_group.Particles);
            */
			//AddComponent(this.RenderState);
            //XXX: Once physics is complete, get the position information from Owner's Body
            
			//Particle Controlling options
			//controller.fadeWithTime = _doFadeWithTime;
			particleGroup.colorFader = _colorFader;
			controller.changeColorWithTime = (_colorFader != null);
            if (controller.changeColorWithTime && _colorFader.FadeList[0].Color != emitter.Color && _colorFader.FadeList[0].FadeTime != 0)
            {
                particleGroup.colorFader.insertColorFader(0, 0, emitter.Color);
            }
			controller.scaleWithTime = _scaleWithTime;

		}

		/// <summary>
		/// Updates the particle emitter and controller.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public override void OnUpdate(object sender, FrameEventArgs e)
		{
			var dt = e.Time;
            particleGroup.Position = Util.ConvertVector2d(Owner.Body.PBody.Position);
            emitter.Update(dt, particleGroup);
            controller.Update(dt, particleGroup);
		}

	}
}

