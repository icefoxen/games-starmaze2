using System;
using Starmaze.Engine;
using OpenTK;

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
	public class Life : Engine.Component
	{
		double CurrentLife { get; set; }

		double MaxLife { get; set; }
		//Multiplier to damage taken
		double DamageAttenuation { get; set; }
		//subtractive damage reduction
		//Applied BEFORE attenuation
		double DamageReduction { get; set; }

		public Life(Engine.Actor owner, double hpsIn, double maxLifeIn = -1.0f, double attenuationIn = 1.0f, double reductionIn = 0.0f) : base(owner)
		{
			//this = new Engine.Component(owner); 
			this.Owner = owner;
			CurrentLife = hpsIn;

			if (maxLifeIn < 0.0f) {
				MaxLife = hpsIn;
			} else {
				MaxLife = maxLifeIn;
			}

			DamageAttenuation = attenuationIn;
			DamageReduction = reductionIn;

		}

		public void TakeDamage(Engine.Actor damager, double damage)
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
				this.Owner.Alive = false;
			}
			//string output = "Took " + attenuatedDamage.ToString() + " out of " + damage.ToString() + " damage, life is now " + CurrentLife.ToString();
			string output = String.Format("Took {0} out of {1} damage, life is now {2}.", attenuatedDamage, damage, CurrentLife);
			Log.Message(output);
		}
	}
	//end Life component
	public class Energy : Engine.Component
	{
		double MaxEnergy { get; set; }

		double CurrentEnergy { get; set; }

		double RegenRate { get; set; }

		public Energy(Engine.Actor owner, double maxEnergy = 100f, double regenRate = 10f) : base(owner)
		{
			MaxEnergy = maxEnergy;
			CurrentEnergy = maxEnergy / 2f;
			RegenRate = RegenRate;
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

		public void Update(double dt)
		{
			if (CurrentEnergy < MaxEnergy) {
				CurrentEnergy += RegenRate * dt;
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

		public TimedLife(Engine.Actor owner, double time) : base(owner)
		{
			Time = time;
			MaxTime = time;
		}

		public void Update(double dt)
		{
			Time -= dt;
			if (Time <= 0) {
				Owner.Alive = false;
			}
		}
	}
	//end TimedLife component
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
	//TODO: ParticleSystem reimplementation seems to depend on implementation of graphics
	//Might belong in Engine.Component instead?
	//public class ParticleSystem : Engine.Component {
	//texture Tex
	//particleGroup
	//}//end ParticleSystem component
}

