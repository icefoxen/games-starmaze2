using System;
using Starmaze.Engine;
using Newtonsoft.Json;
using OpenTK;
using OpenTK.Input;

namespace Starmaze.Game
{
	public class Timer
	{
		double Time { get; set; }

		double DefaultTime { get; set; }

		public Timer(double time = 0f, double defaultTime = 0f)
		{
			Time = time;
			DefaultTime = defaultTime;
		}

		public void Reset()
		{
			Time = DefaultTime;
		}

		public void Update(double dt)
		{
			Time -= dt;
		}

		public bool Expired()
		{
			return(Time <= 0f);
		}
	}
	//end Timer
	public class Life : Component
	{
		public double CurrentLife { get; set; }

		public double MaxLife { get; set; }

		/// <summary>
		/// Multiplier to damage taken, generally <1.0
		/// </summary>
		/// <value>The damage attenuation.</value>
		public double DamageAttenuation { get; set; }

		/// <summary>
		/// subtractive damage reduction
		/// Applied BEFORE attenuation
		/// </summary>
		/// <value>The damage reduction.</value>
		public double DamageReduction { get; set; }

		public Life(double hpsIn, double maxLifeIn = -1.0f, double attenuationIn = 1.0f, double reductionIn = 0.0f) : base()
		{
			CurrentLife = hpsIn;

			if (maxLifeIn < 0.0f) {
				MaxLife = hpsIn;
			} else {
				MaxLife = maxLifeIn;
			}

			DamageAttenuation = attenuationIn;
			DamageReduction = reductionIn;
		}

		public void TakeDamage(Actor damager, double damage)
		{
			double reducedDamage = Math.Max(0, damage - DamageReduction);
			double attenuatedDamage = reducedDamage - DamageReduction;
			/*TODO: Rewrite sound to fit with OpenTK
			 * 	if attenuatedDamage >=4:
             * 		rcache.get_sound("damage_4").play()
             * 	if attenuatedDamage >=3:
             *		rcache.get_sound("damage_3").play()
        	 *	elif attenuatedDamage>0:
             *		rcache.get_sound("damage").play()
        	 *	else:
             *		rcache.get_sound("damage_0").play()
			 */
			CurrentLife -= attenuatedDamage;
			if (CurrentLife <= 0) {
				Owner.Alive = false;
				Owner.World.TriggerOnDeath(this, EventArgs.Empty);
			}
			//string output = "Took " + attenuatedDamage.ToString() + " out of " + damage.ToString() + " damage, life is now " + CurrentLife.ToString();
			string output = String.Format("Took {0} out of {1} damage, life is now {2}.", attenuatedDamage, damage, CurrentLife);
			Log.Message(output);
		}

		public override string ToString()
		{
			return string.Format("Life(CurrentLife: {0}, MaxLife: {1}, DamageAttenuation: {2}, DamageReduction: {3})", CurrentLife, MaxLife, DamageAttenuation, DamageReduction);
		}
	}
	//end Life component
	public class Energy : Component
	{
		public double MaxEnergy { get; set; }

		double CurrentEnergy { get; set; }

		public double RegenRate { get; set; }

		public Energy(double maxEnergy = 100f, double regenRate = 10f) : base()
		{
			MaxEnergy = maxEnergy;
			CurrentEnergy = maxEnergy / 2f;
			RegenRate = RegenRate;

			HandledEvents = EventType.OnUpdate;
		}

		public bool Expend(double amount)
		{
			if (amount <= CurrentEnergy) {
				CurrentEnergy -= amount;
				return true;
			} else {
				return false;
			}
		}

		public void OnUpdate(FrameEventArgs e)
		{
			if (CurrentEnergy < MaxEnergy) {
				CurrentEnergy += RegenRate * e.Time;
			}
			if (CurrentEnergy > MaxEnergy) {
				CurrentEnergy = MaxEnergy;
			}
		}
	}
	//end Energy component
	public class TimedLife : Component
	{
		public double Time { get; set; }

		public double MaxTime { get; set; }

		public TimedLife(double time) : base()
		{
			Time = time;
			MaxTime = time;

			HandledEvents = EventType.OnUpdate;
		}

		public void OnUpdate(FrameEventArgs e)
		{
			var dt = e.Time;
			Time -= dt;
			Owner.Alive &= Time > 0;
		}
	}
	// Check out this awesomeness: http://gameprogrammingpatterns.com/state.html
	/// <summary>
	/// A Component that fires bullets of various types.
	/// </summary>
	public class Gun : Component
	{
		public Vector2d FireOffset { get; set; }

		public Gun() : base()
		{
			FireOffset = Vector2d.Zero;
		}
	}

	/// <summary>
	/// A Component that moves an Actor around and such based on user inputs.
	/// </summary>
	public class InputController : Component
	{

		public InputController() : base()
		{
			HandledEvents = EventType.OnKeyDown | EventType.OnKeyUp;
		}

		public override void OnKeyDown(object sender, InputAction a)
		{
			Log.Message("Key down: {0}", a);

			switch (a) {
				case InputAction.MoveUp:
					Owner.Body.PBody.ApplyLinearImpulse(new Microsoft.Xna.Framework.Vector2(0, 500));
					Resources.Sound.PlaySound(Resources.TheResources.GetSound("Powers_Air_Wave_Small.wav"));
					//Owner.Body.AddVelocity(new Vector2d(0, 5));
					break;
				case InputAction.MoveDown:
					Owner.Body.PBody.ApplyLinearImpulse(new Microsoft.Xna.Framework.Vector2(0, -500));
					//Owner.Body.AddVelocity(new Vector2d(0, -5));
					break;
				case InputAction.MoveLeft:
					Owner.Body.PBody.ApplyLinearImpulse(new Microsoft.Xna.Framework.Vector2(-500, 0));
					//Owner.Body.AddVelocity(new Vector2d(-5, 0));
					break;
				case InputAction.MoveRight:
					Owner.Body.PBody.ApplyLinearImpulse(new Microsoft.Xna.Framework.Vector2(500, 0));
					//Owner.Body.AddVelocity(new Vector2d(5, 0));
					break;
				case InputAction.SoundUp:
					Resources.Sound.Volume = Math.Min(Resources.Sound.Volume + 0.1f, 1.0f);
					break;
				case InputAction.SoundDown:
					Resources.Sound.Volume = Math.Max(Resources.Sound.Volume - 0.1f, 0.0f);
					break;
			}
		}

		public override void OnKeyUp(object sender, InputAction a)
		{

		}
	}
	//TODO: ParticleSystem reimplementation seems to depend on implementation of graphics
	//Might belong in Component instead?
	//public class ParticleSystem : Component {
	//texture Tex
	//particleGroup
	//}//end ParticleSystem component
}

