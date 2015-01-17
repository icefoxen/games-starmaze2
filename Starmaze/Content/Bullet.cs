using System;
using Starmaze.Engine;

namespace Starmaze.Content
{
	public class TrooperBullet : Actor
	{
		Actor firer;
		Game.TimedLife life;
		float damage;
		float rotationSpeed;

		public TrooperBullet(Actor firerIn) : base()
		{
			firer = firerIn;
			life = new Starmaze.Game.TimedLife(this, 1f);

			float xImpulse = 300f * (float) Facing;
			float yImpulse = 0f;

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

