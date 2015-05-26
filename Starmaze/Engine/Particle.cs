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
		ParticleEmitter Shape;

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
		public void Update(double dt, ref ParticleGroup group)
		{
			var fdt = (float)dt;
			List<Particle> list = group.Particles;
			Color4 newColor;
			float colorRate = 0.8f * fdt;
			Color4 nextColor = Color4.White;

			for (int i = list.Count - 1; i > -1; i--) {
				var p = list[i];

				if (scaleWithTime) {
					p.Scale += (deltaScale * fdt);
					p.Scale = (float)SMath.Clamp(p.Scale, 0.1, 5);
				}
				if (changeColorWithTime) {
					group.colorFader.setColor(ref p, ref nextColor, ref colorRate);
					colorRate *= (float)dt;

					p.Color.R -= (p.Color.R - nextColor.R) * colorRate;
					p.Color.G -= (p.Color.G - nextColor.G) * colorRate;
					p.Color.B -= (p.Color.B - nextColor.B) * colorRate;
					p.Color.A -= (p.Color.A - nextColor.A) * colorRate;
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
				p.Velocity += _gravity * dt;
				//*(velocity * rand.NextDouble() * dt);
				var speed = p.Velocity * dt;
				var pos = Vector2d.Zero;
				//Vector2d.Multiply(ref vel, ref _gravity, out vel);

				//Final Step - add the calculated Velocity the particle's position
				Vector2d.Add(ref p.Position, ref speed, out pos);
				p.Position = pos;
				p.Life -= dt;
				list[i] = p;

				if (p.Life < 0) {
					group.Remove(i);
				}
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
		public struct ColorFade
		{
			public double threshold;
			public Color4 color;
		}
        
		//The List of color fades
		List<ColorFade> list;

		public List<ColorFade> ColorFaders {
			get { return list; }
		}

		/// <summary>
		/// Makes a ColorFader, Must have the 1st ColorFader's threshold set to 0.
		/// The order the Dictionary entries will be the order of the colors for the Fader (at least for now)
		/// </summary>
		/// <param name="dictionary"> double (the key and the threshold) is about how many seconds for the particle to completely become that color
		/// , Color4 is the color for that </param>
		public ColorFader(Dictionary<double,Color4> dictionary)
		{
			ColorFade cf;
			//giving it a starting capacity of 4, don't think we should need more than that
			list = new List<ColorFade>(4);
			foreach (var item in dictionary) {
				cf.color = item.Value;
				cf.threshold = item.Key;
				list.Add(cf);
			}
		}

		public ColorFader(List<ColorFade> colorFades)
		{
			list = colorFades;
		}

		/// <summary>
		///Sets the Next Color for the particle to fade to. It also updates the particles Color Fader index
		///and sets the rate to change between colors
		/// </summary>
		/// <param name="particle"></param>
		public void setColor(ref Particle particle, ref Color4 nextcolor, ref float colorRate)
		{
			if (particle.ColorFadeIndex + 1 < list.Count && particle.timeAlive >= list[particle.ColorFadeIndex].threshold) {
				particle.ColorFadeIndex += 1;
			}

			//colorRate = (float)((list[particle.cfIndex].threshold - particle.timeAlive) / list[particle.cfIndex].threshold);
			colorRate = (float)(particle.MaxLife / (list[particle.ColorFadeIndex].threshold - list[particle.ColorFadeIndex - 1].threshold));
			nextcolor = list[particle.ColorFadeIndex].color;
		}

		/// <summary>
		/// Returns a Dictionary<double,Color4> of all the colors with their threshold
		/// </summary>
		/// <returns>A dictionary of the Colors and their thresholds </returns>
		public Dictionary<double,Color4> getColorFaders()
		{
			var dictionary = new Dictionary<double, Color4>();
            
			foreach (ColorFade colorfade in list) {
				dictionary.Add(colorfade.threshold, colorfade.color);
			}
			return dictionary;
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
		public CircleEmitter(Color4 color, float radius = 5f, int start_angle = 0, int end_angle = 360, double velocityMagnitude = 3f, double emitDelay = 0.1, double maxLifeTime = 3f)
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
			particle_group.AddParticle(position, angleVec, color, maxLifeTime);
			//}
		}

	}

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
		public LineEmitter(Color4 color, float length = 1f, int angle = 0, double velocityMagnitude = 1f, double emitDelay = 0.1, double maxLifeTime = 1f)
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
				position = new Vector2d(Math.Cos(angle * Math.PI / 180), Math.Sin(angle * Math.PI / 180));
				Vector2d.Normalize(ref position, out angleVec);
				//Log.Message(String.Format("Particle Angle {0} , {1} , {2}", current_angle, position.X, position.Y));
				particle_group.AddParticle(position, angleVec, color, maxLifeTime);
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
			double xV = rand.NextDouble() * velocity.X * direction;
			direction *= -1;
			//double yV = rand.NextDouble() * velocity.Y;
            
			//Will Add Code Soon
			Vector2d position = Vector2d.Zero, angleVec = new Vector2d(xV, velocity.Y);
         
			//current_angle = start_angle * Math.PI / 180;

			lastTime += dt;
			//for (int i = 0; i <= 360 && lastTime >= nextTime; i += 10, current_angle++)
			while (lastTime >= nextTime) {
				nextTime += emitDelay;
				//position = new Vector2d(Math.Cos(angle * Math.PI / 180), Math.Sin(angle * Math.PI / 180));
				Vector2d.Normalize(ref angleVec, out angleVec);
				particle_group.AddParticle(position, angleVec, color, maxLifeTime);
                
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
		public Body Body;
		Vector2d position = Vector2d.Zero;

		public ParticleGroup(int MaxParticles)
		{
			Particles = new List<Particle>(MaxParticles);
		}

		public void AddParticle(Particle p)
		{
			Particles.Add(p);
		}

		public void AddParticle(Vector2d pos, Vector2d velocity, Color4 color, double life = 1.0)
		{
			Particles.Add(new Particle(pos + position, velocity: velocity, color: color, life: life));
			//Log.Message(String.Format("Particle ({0}) V {0}", pos, vel));
		}

		public void Update()
		{
			//position.X = Body.Position.X;
			//position.Y = Body.Position.Y;
			//position = Util.ConvertVector2d(Body.PBody.Position);
			//Log.Message(" " + position);
            
		}

		/// <summary>
		/// Removes a particle 
		/// </summary>
		/// <param name="i">the index of the particle to remove</param>
		public void Remove(int i)
		{
			Particles[i] = Particles[Particles.Count - 1];
			Particles.RemoveAt(Particles.Count - 1);
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
		ParticleGroup particleGroup;
		public double velocityMagnitude;
		public int maxParticles;
		public float gravity, deltaScale;
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
			particleGroup = new ParticleGroup(MaxParticles);

			Texture texture = Resources.TheResources.GetTexture("dot");
			this.RenderState = new ParticleRenderState(texture, particleGroup.Particles);
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
			particleGroup.Body = Owner.Body;
			//Particle Controlling options
			//controller.fadeWithTime = _doFadeWithTime;
			particleGroup.colorFader = _colorFader;
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
			particleGroup.Update();
			emitter.Update(dt, ref particleGroup);
			((ParticleRenderState)this.RenderState).particleList = particleGroup.Particles;
			controller.Update(dt, ref particleGroup);
		}

	}
}

