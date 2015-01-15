using System;

namespace Starmaze.Content
{
	class CrawlerEnemy : Engine.Actor {
		//TODO: Incomplete port of python CrawlerEnemy

		//Controller = new RoamAIController;
		//PhysicsObject = CrawlerPhysicsObject;
		//Render = rchace.getRenderer(CrawlerReder);
		Game.Life life;


		CrawlerEnemy() {
			life = new Starmaze.Game.Life(this, 3f, 3f, 1f, 8f);
		}

		public void Update(float dt){
			//controller.update(dt)
		}
		public void OnDeath(){
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

	class TrooperEnemy : Engine.Actor {
		//TODO: Incomplete port of python TrooperEnemy
		Game.Life life;

		TrooperEnemy(){
			//s.bulletOffset = (30,0)
			life = new Starmaze.Game.Life(this, 100);
		}
	} //end TrooperEnemy

	class ArcherEnemy : Engine.Actor {
		//TODO: Incomplete port of python ArcherEnemy
		Game.Life life;

		ArcherEnemy(){
			//s.bulletOffset = (25, 0)
			life = new Starmaze.Game.Life(this, 20);
		}
	}//end ArcherEnemy

	class FloaterEnemy : Engine.Actor {
		//TODO: Incomplete port of python FloaterEnemy
		Game.Life life;

		FloaterEnemy(){
			life = new Starmaze.Game.Life(this, 20);
		}
	} // end FloaterEnemy

	class EliteEnemy : Engine.Actor {
		//TODO: Incompelte port of python EliteEnemy
		Game.Life life;

		EliteEnemy(){
			life = new Starmaze.Game.Life(this, 10);
		}
	} //end EliteEnemy
}

