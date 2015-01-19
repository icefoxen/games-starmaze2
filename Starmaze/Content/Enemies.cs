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
		Life life;

		CrawlerEnemy()
		{
			life = new Starmaze.Game.Life(this, 3f, 3f, 1f, 8f);

		}

		public void Update(float dt)
		{
			//controller.update(dt)
		}

		public override void OnDeath()
		{
			/*
			 *	c = Collectable()
        	 *	c.physicsObj.position = s.physicsObj.position
        	 *	yForce = 350
        	 *	xForce = (random.random() * 150) - 75
        	 *	c.physicsObj.apply_impulse((xForce, yForce))
        	 *	s.world.addActor(c)
			 */
		}
	}
	//end CralwerEnemy
	class TrooperEnemy : Actor
	{
		//TODO: Incomplete port of python TrooperEnemy
		Game.Life life;

		TrooperEnemy()
		{
			//s.bulletOffset = (30,0)
			life = new Starmaze.Game.Life(this, 100);
		}
	}
	//end TrooperEnemy
	class ArcherEnemy : Actor
	{
		//TODO: Incomplete port of python ArcherEnemy
		Game.Life life;

		ArcherEnemy()
		{
			//s.bulletOffset = (25, 0)
			life = new Starmaze.Game.Life(this, 20);
		}
	}
	//end ArcherEnemy
	class FloaterEnemy : Actor
	{
		//TODO: Incomplete port of python FloaterEnemy
		Game.Life life;

		FloaterEnemy()
		{
			life = new Starmaze.Game.Life(this, 20);
		}
	}
	// end FloaterEnemy
	class EliteEnemy : Actor
	{
		//TODO: Incompelte port of python EliteEnemy
		Game.Life life;

		EliteEnemy()
		{
			life = new Starmaze.Game.Life(this, 10);
		}
	}
	//end EliteEnemy
	class HeavyEnemy : Actor
	{
		//TODO: Incomplete port of the python HeavyEnemy
		Game.Life life;

		HeavyEnemy()
		{
			life = new Starmaze.Game.Life(this, 10);
		}
	}
	//end HeavyEnemy
	class DragonEnemy : Actor
	{
		//TODO: Incomplete port of the python DragonEnemy
		Game.Life life;

		DragonEnemy()
		{
			life = new Starmaze.Game.Life(this, 10);
		}
	}
	//end DragonEnemy
	class AnnihilatorEnemy : Actor
	{
		//TODO: Incomplete port of the python Enemy
		Game.Life life;

		AnnihilatorEnemy()
		{
			life = new Starmaze.Game.Life(this, 10);
		}
	}
	//end AnnihilatorEnemy
}