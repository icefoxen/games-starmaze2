using System;
using Starmaze.Engine;
using Starmaze.Game;
using OpenTK;

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

			double xImpulse = 300 * (int)Body.Facing;
			double yImpulse = 0f;
			Body.AddImpulse(new Vector2d(xImpulse, yImpulse));

			damage = 6f;
			rotationSpeed = 10f;
		}
	}
}

