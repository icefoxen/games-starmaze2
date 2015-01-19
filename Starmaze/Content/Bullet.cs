using System;
using Starmaze.Engine;
using Starmaze.Game;

namespace Starmaze.Content
{
	public class TrooperBullet : Actor
	{
		Actor firer;
		TimedLife life;
		double damage;
		double rotationSpeed;

		public TrooperBullet(Actor firerIn) : base()
		{
			firer = firerIn;
			life = new TimedLife(this, 1f);

			double xImpulse = 300 * (int)Facing;
			double yImpulse = 0f;

			damage = 6f;
			rotationSpeed = 10f;
		}

		public override void Update(double dt)
		{
			this.life.Update(dt);
			//s.physicsObj.angle += dt * s.rotateSpeed
		}
	}
}

