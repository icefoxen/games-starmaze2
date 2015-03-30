using System;
using Starmaze.Engine;
using Starmaze.Game;

namespace Starmaze.Content
{
	// XXX: TODO: Refactor a little bit; lots of Actor types will have a Life,
	// probably a Gun, etc.  One question is, should it include the Player as well?
	// various traps?  Can we refactor this out to a Component?  Do we just want
	// _every_ Actor to have slots for Life, Gun, and other things, even if lots of
	// things (like Terrain and Collectables) won't ever use them?
	class CrawlerEnemy : Actor
	{
		//TODO: Incomplete port of python CrawlerEnemy
		//Controller = new RoamAIController;
		//PhysicsObject = CrawlerPhysicsObject;
		//Render = rchace.getRenderer(CrawlerReder);
		CrawlerEnemy()
		{
			AddComponent(new Life(this, 3f, 3f, 1f, 8f));

		}
		/*
		public override void OnDeath()
		{
			 c = Collectable()
        	 c.physicsObj.position = s.physicsObj.position
        	 yForce = 350
        	 xForce = (random.random() * 150) - 75
        	 c.physicsObj.apply_impulse((xForce, yForce))
        	 s.world.addActor(c)
		}
			 */
	}

	class TrooperEnemy : Actor
	{
		//TODO: Incomplete port of python TrooperEnemy
		TrooperEnemy()
		{
			//s.bulletOffset = (30,0)
			AddComponent(new Life(this, 100));
		}
	}

	class ArcherEnemy : Actor
	{
		//TODO: Incomplete port of python ArcherEnemy
		ArcherEnemy()
		{
			//s.bulletOffset = (25, 0)
			AddComponent(new Life(this, 20));
		}
	}

	class FloaterEnemy : Actor
	{
		//TODO: Incomplete port of python FloaterEnemy
		FloaterEnemy()
		{
			AddComponent(new Life(this, 20));
		}
	}

	class EliteEnemy : Actor
	{
		//TODO: Incomplete port of python EliteEnemy
		EliteEnemy()
		{
			AddComponent(new Life(this, 10));
		}
	}

	class HeavyEnemy : Actor
	{
		//TODO: Incomplete port of the python HeavyEnemy
		HeavyEnemy()
		{
			AddComponent(new Life(this, 10));
		}
	}

	class DragonEnemy : Actor
	{
		//TODO: Incomplete port of the python DragonEnemy
		DragonEnemy()
		{
			AddComponent(new Life(this, 10));
		}
	}

	class AnnihilatorEnemy : Actor
	{
		//TODO: Incomplete port of the python Enemy
		AnnihilatorEnemy()
		{
			AddComponent(new Life(this, 10));
		}
	}
}