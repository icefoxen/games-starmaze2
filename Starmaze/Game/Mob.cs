using System;

namespace Starmaze.Game
{
	public class Mob
	{
		public Mob()
		{
		}
	}


	public class Life : Engine.Component{
		float CurrentLife { get; set; }
		float MaxLife { get; set; }
		//Multiplier to damage taken
		float DamageAttenuation { get; set; } 
		//subtractive damage reduction
		//Applied BEFORE attenuation
		float DamageReduction { get; set; } 

		public Life(Engine.Actor owner, float hpsIn, float maxLifeIn = -1.0f, float attenuationIn = 1.0f, float reductionIn = 0.0f) : base(owner){
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
		public void takeDamage(Engine.Actor damager, float damage){
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
			string output = "Took " + attenuatedDamage.ToString() + " out of " + damage.ToString() + " damage, life is now " + CurrentLife.ToString();
		}
	} //end Life component
}

