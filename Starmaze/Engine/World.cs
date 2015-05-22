using System;
using System.Collections.Generic;
using System.Linq;
using Starmaze.Engine;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using FarseerPhysics;
using FarseerPhysics.Dynamics;

namespace Starmaze.Engine
{
	/// <summary>
	/// This is the object that actually *runs* the game, holds on to all the Actors,
	/// handles the various components and subsystems like graphics and rendering, etc.
	/// </summary>
	public class World
	{
		WorldMap Map;
		Room CurrentRoom;
		RenderManager RenderManager;
		HashSet<Actor> Actors;
		List<Actor> ActorsToAdd;
		List<Actor> ActorsToRemove;
		FarseerPhysics.Dynamics.World PhysicsWorld;

		public event EventHandler<FrameEventArgs> OnUpdate;
		public event EventHandler<InputAction> OnKeyDown;
		public event EventHandler<InputAction> OnKeyUp;
		public event EventHandler<EventArgs> OnDeath;
		//		public ParticleGroup grp;
		ParticleController cont;
		ParticleRenderer rend;
		//ParticleEmitter emit;
		Actor player;
		//FarseerPhysics.Dynamics.Body bod;

		public World(Actor player, WorldMap map, string initialZone, string initialRoom)
		{
			// ClearWorld handles a lot of our initialization
			ClearWorld();
			Map = map;
			ChangeRoom(Map[initialZone][initialRoom]);
			AddActor(player);
			player.AddComponent(new Body());
			this.player = player;

//			var act = new Actor();
//			act.Body = new FBody(act, bodyType: BodyType.Static);
//			act.Body.Position = new Vector2d(0, -30);
//			act.RenderState = new ModelRenderState(act, Starmaze.Content.Images.FilledRectCenter(0, 0, 10, 5, Color4.Red));
//			AddActor(act);
//
//			grp = new ParticleGroup();
		}

		/// <summary>
		/// This essentially clears all objects from the world.
		/// Used when moving from one room to another,
		/// when we switch out all objects for new ones.
		/// </summary>
		// OPT: Someday might be a good idea to just empty data structures instead of
		// freeing and re-allocating them.
		public void ClearWorld()
		{
			// BUGGO: This should use a real resolution.
			RenderManager = new RenderManager(1024, 768);
			PhysicsWorld = new FarseerPhysics.Dynamics.World(Util.Gravity);

			Actors = new HashSet<Actor>();
			ActorsToAdd = new List<Actor>();
			ActorsToRemove = new List<Actor>();
		}

		public void ChangeRoom(Room newRoom)
		{
			var preservedActors = Actors.Where(act => act.KeepOnRoomChange);
			ClearWorld();
			foreach (var act in newRoom.ReifyActorsForEntry()) {
				AddActor(act);
			}
			foreach (var act in preservedActors) {
				AddActor(act);
			}
			CurrentRoom = newRoom;
		}

		public void AddActor(Actor a)
		{
			ActorsToAdd.Add(a);
		}

		public void RemoveActor(Actor a)
		{
			ActorsToRemove.Add(a);
		}

		void ImmediateAddActor(Actor a)
		{
			Actors.Add(a);
			a.World = this;
			RenderManager.Add(a);
			//Log.Message("Body: {0}, actor {1}, physics world: {2}", a.Body, a, PhysicsWorld);
			if (a.Body != null) {
				a.Body.AddToWorld(PhysicsWorld);
			}
			a.RegisterEvents(this);
		}

		void ImmediateRemoveActor(Actor a)
		{
			Actors.Remove(a);
			RenderManager.Remove(a);
			if (a.Body != null) {
				a.Body.RemoveFromWorld(PhysicsWorld);
			}
			a.UnregisterEvents(this);
		}

		public void Update(FrameEventArgs e)
		{
			var dt = e.Time;
			//Space.Update(dt);
			PhysicsWorld.Step((float)dt);
			// If nothing is listening for an event it will be null
			if (OnUpdate != null) {
				OnUpdate(this, e);
			}
			foreach (var act in ActorsToAdd) {
				ImmediateAddActor(act);
			}
			foreach (var act in ActorsToRemove) {
				ImmediateRemoveActor(act);
			}

			ActorsToAdd.Clear();
			ActorsToRemove.Clear();

			//emit.Update(dt, grp);
			//cont.Update(dt, grp);
		}

		public void Draw(ViewManager view)
		{
			RenderManager.Render(view);
			//rend.Draw(view, grp);
		}

		public void Resize(int width, int height)
		{
			RenderManager.Resize(width, height);
		}

		public void HandleKeyDown(InputAction a)
		{
			// Okay, the 'event handler with nothing attached == null' idiom is a pain in the ass
			if (OnKeyDown != null) {
				OnKeyDown(this, a);
			}
			/*
			 * Test code to make sure ChangeRoom() works; it does.
			if (e.Key == OpenTK.Input.Key.Number1) {
				ChangeRoom(Map["TestZone"]["TestRoom1"]);
			} else if (e.Key == OpenTK.Input.Key.Number2) {
				ChangeRoom(Map["TestZone"]["TestRoom2"]);
			}
			*/
		}

		public void HandleKeyUp(InputAction a)
		{
			if (OnKeyUp != null) {
				OnKeyUp(this, a);
			}
		}
		// Hrmbl grmbl C# blrgl not letting random classes invoke events
		// Which is to say, if any event needs to be triggered by something other than the World
		// itself, it needs a method like this to make it accessible.
		public void TriggerOnDeath(object sender, EventArgs e)
		{
			if (OnDeath != null) {
				OnDeath(sender, e);
			}
		}
	}
}

