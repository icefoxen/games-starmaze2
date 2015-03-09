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
		[JsonProperty]
		double CurrentLife { get; set; }

		[JsonProperty]
		double MaxLife { get; set; }
		//Multiplier to damage taken

		[JsonProperty]
		double DamageAttenuation { get; set; }
		//subtractive damage reduction
		//Applied BEFORE attenuation
		[JsonProperty]
		double DamageReduction { get; set; }

		public Life(Actor owner, double hpsIn, double maxLifeIn = -1.0f, double attenuationIn = 1.0f, double reductionIn = 0.0f) : base(owner)
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
		double MaxEnergy { get; set; }

		double CurrentEnergy { get; set; }

		double RegenRate { get; set; }

		public Energy(Actor owner, double maxEnergy = 100f, double regenRate = 10f) : base(owner)
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
		double Time { get; set; }

		double MaxTime { get; set; }

		public TimedLife(Actor owner, double time) : base(owner)
		{
			Time = time;
			MaxTime = time;

			HandledEvents = EventType.OnUpdate;
		}

		public void OnUpdate(FrameEventArgs e)
		{
			var dt = e.Time;
			Time -= dt;
			if (Time <= 0) {
				Owner.Alive = false;
			}
		}
	}
	// Check out this awesomeness: http://gameprogrammingpatterns.com/state.html
	/// <summary>
	/// A Component that fires bullets of various types.
	/// </summary>
	public class Gun : Component
	{
		public Vector2d fireOffset;

		public Gun(Actor owner) : base(owner)
		{
			fireOffset = Vector2d.Zero;
		}
	}

	/// <summary>
	/// A Component that moves an Actor around and such based on user inputs.
	/// </summary>
	public class InputController : Component
	{

		public InputController(Actor owner) : base(owner)
		{
			HandledEvents = EventType.OnKeyDown | EventType.OnKeyUp;
		}

		public override void OnKeyDown(object sender, InputAction a)
		{
			Log.Message("Key down: {0}", a);

			switch (a) {
				case InputAction.MoveUp:
					Owner.Body.AddVelocity(new Vector2d(0, 5));
					break;
				case InputAction.MoveDown:
					Owner.Body.AddVelocity(new Vector2d(0, -5));
					break;
				case InputAction.MoveLeft:
					Owner.Body.AddVelocity(new Vector2d(-5, 0));
					break;
				case InputAction.MoveRight:
					Owner.Body.AddVelocity(new Vector2d(5, 0));
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

