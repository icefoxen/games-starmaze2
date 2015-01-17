using System;
using Starmaze.Engine;

namespace Starmaze.Game
{
	public class Mob
	{
		public Mob()
		{
		}
	}

	public class Timer
	{
		float Time { get; set; }

		float DefaultTime { get; set; }

		public Timer(float time = 0f, float defaultTime = 0f)
		{
			Time = time;
			DefaultTime = defaultTime;
		}

		public void Reset()
		{
			Time = DefaultTime;
		}

		public void Update(float dt)
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
		float CurrentLife { get; set; }

		float MaxLife { get; set; }
		//Multiplier to damage taken
		float DamageAttenuation { get; set; }
		//subtractive damage reduction
		//Applied BEFORE attenuation
		float DamageReduction { get; set; }

		public Life(Engine.Actor owner, float hpsIn, float maxLifeIn = -1.0f, float attenuationIn = 1.0f, float reductionIn = 0.0f) : base(owner)
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

		public void TakeDamage(Engine.Actor damager, float damage)
		{
			float reducedDamage = Math.Max(0, damage - DamageReduction);
			float attenuatedDamage = reducedDamage - DamageReduction;
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
		float MaxEnergy { get; set; }

		float CurrentEnergy { get; set; }

		float RegenRate { get; set; }

		public Energy(Engine.Actor owner, float maxEnergy = 100f, float regenRate = 10f) : base(owner)
		{
			MaxEnergy = maxEnergy;
			CurrentEnergy = maxEnergy / 2f;
			RegenRate = RegenRate;
		}

		public bool Expend(float amount)
		{
			if (amount <= CurrentEnergy) {
				CurrentEnergy -= amount;
				return true;
			} else {
				return false;
			}
		}

		public void Update(float dt)
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

	public class TimedLife : Engine.Component
	{
		float Time { get; set; }

		float MaxTime { get; set; }

		public TimedLife(Engine.Actor owner, float time) : base(owner)
		{
			Time = time;
			MaxTime = time;
		}

		public void Update(float dt)
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
		public Gun(Actor owner) : base(owner)
		{

		}
	}

	//TODO: ParticleSystem reimplementation seems to depend on implementation of graphics
	//Might belong in Engine.Component instead?
	//public class ParticleSystem : Engine.Component {
	//texture Tex
	//particleGroup
	//}//end ParticleSystem component
}

