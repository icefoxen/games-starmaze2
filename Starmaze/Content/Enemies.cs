using System;
using Starmaze.Engine;

namespace Starmaze.Content
{
	class CrawlerEnemy : Actor {
		//TODO: Incomplete port of python CrawlerEnemy

		//Controller = new RoamAIController;
		//PhysicsObject = CrawlerPhysicsObject;
		//Render = rchace.getRenderer(CrawlerReder);
		Game.Life life;
		
		CrawlerEnemy() {
			Facing = Facing.Left;
			life = new Starmaze.Game.Life(this, 3f, 3f, 1f, 8f);

		}

		public void Update(float dt){
			//controller.update(dt)
		}
		public override void OnDeath(){
			/*
			 *	c = Collectable()
        	 *	c.physicsObj.position = s.physicsObj.position
        	 *	yForce = 350
        	 *	xForce = (random.random() * 150) - 75
        	 *	c.physicsObj.apply_impulse((xForce, yForce))
        	 *	s.world.addActor(c)
			 */
		}
	} //end CralwerEnemy

	class TrooperEnemy : Actor {
		//TODO: Incomplete port of python TrooperEnemy
		Game.Life life;

		TrooperEnemy(){
			//s.bulletOffset = (30,0)
			Facing = Facing.Left;
			life = new Starmaze.Game.Life(this, 100);
		}
	} //end TrooperEnemy

	class ArcherEnemy : Actor {
		//TODO: Incomplete port of python ArcherEnemy
		Game.Life life;

		ArcherEnemy(){
			//s.bulletOffset = (25, 0)
			Facing = Facing.Left;
			life = new Starmaze.Game.Life(this, 20);
		}
	}//end ArcherEnemy

	class FloaterEnemy : Actor {
		//TODO: Incomplete port of python FloaterEnemy
		Game.Life life;

		FloaterEnemy(){
			Facing = Facing.Left;
			life = new Starmaze.Game.Life(this, 20);
		}
	} // end FloaterEnemy

	class EliteEnemy : Actor {
		//TODO: Incompelte port of python EliteEnemy
		Game.Life life;

		EliteEnemy(){
			Facing = Facing.Left;
			life = new Starmaze.Game.Life(this, 10);
		}
	} //end EliteEnemy

	class HeavyEnemy : Actor {
		//TODO: Incomplete port of the python HeavyEnemy
		Game.Life life;

		HeavyEnemy() {
			Facing = Facing.Left;
			life = new Starmaze.Game.Life(this, 10);
		}
	} //end HeavyEnemy

	class DragonEnemy : Actor {
		//TODO: Incomplete port of the python DragonEnemy
		Game.Life life;

		DragonEnemy() {
			Facing = Facing.Left;
			life = new Starmaze.Game.Life(this, 10);
		}
	} //end DragonEnemy

	class AnnihilatorEnemy : Actor {
		//TODO: Incomplete port of the python Enemy
		Game.Life life;

		AnnihilatorEnemy() {
			Facing = Facing.Left;
			life = new Starmaze.Game.Life(this, 10);
		}
	} //end AnnihilatorEnemy

}