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
		Space Space;
		HashSet<Actor> Actors;
		HashSet<Actor> ActorsToAdd;
		HashSet<Actor> ActorsToRemove;
		FarseerPhysics.Dynamics.World world;
		FarseerPhysics.Dynamics.Body body;
		FarseerPhysics.Collision.Shapes.Shape shape;
		FarseerPhysics.Dynamics.Fixture fixture;
		FarseerPhysics.Dynamics.Body body2;
		FarseerPhysics.Collision.Shapes.Shape shape2;
		FarseerPhysics.Dynamics.Fixture fixture2;

		public event EventHandler<FrameEventArgs> OnUpdate;
		public event EventHandler<InputAction> OnKeyDown;
		public event EventHandler<InputAction> OnKeyUp;
		public event EventHandler<EventArgs> OnDeath;
		//		public ParticleGroup grp;
		ParticleController cont;
		ParticleRenderer rend;
		ParticleEmitter emit;
		Actor player;

		public World(Actor player, WorldMap map, string initialZone, string initialRoom)
		{
			Actors = new HashSet<Actor>();
			ActorsToAdd = new HashSet<Actor>();
			ActorsToRemove = new HashSet<Actor>();
			Space = new Space();

			// BUGGO: This should use a real resolution.
			RenderManager = new RenderManager(1024, 768);

			Map = map;
			ChangeRoom(Map[initialZone][initialRoom]);
			AddActor(player);
			this.player = player;

//			grp = new ParticleGroup();
			cont = new ParticleController();
			rend = new ParticleRenderer();
			emit = new ParticleEmitter(0.001);

			world = new FarseerPhysics.Dynamics.World(new Microsoft.Xna.Framework.Vector2(0, -1f));
			body = new FarseerPhysics.Dynamics.Body(world);
			body.BodyType = BodyType.Dynamic;
			shape = new FarseerPhysics.Collision.Shapes.CircleShape(5, 1);
			fixture = body.CreateFixture(shape);

			body2 = FarseerPhysics.Factories.BodyFactory.CreateRectangle(world, 20, 1, 1);
			body2.BodyType = BodyType.Static;
			body2.Position = new Microsoft.Xna.Framework.Vector2(0, -10f);
		}

		/// <summary>
		/// This essentially clears all objects from the world.
		/// Used when moving from one room to another,
		/// when we switch out all objects for new ones.
		/// </summary>
		public void ClearWorld()
		{
			Space = new Space();
			// BUGGO: This should use a real resolution.
			RenderManager = new RenderManager(1024, 768);

			Actors = new HashSet<Actor>();
			ActorsToAdd = new HashSet<Actor>();
			ActorsToRemove = new HashSet<Actor>();
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
			Space.Add(a.Body);
			a.RegisterEvents(this);
		}

		void ImmediateRemoveActor(Actor a)
		{
			Actors.Remove(a);
			RenderManager.Remove(a);
			Space.Remove(a.Body);
			a.UnregisterEvents(this);
		}

		public void Update(FrameEventArgs e)
		{
			var dt = e.Time;
			Space.Update(dt);
			world.Step((float)dt);
			player.Body.Position = new Vector2d(body.Position.X, body.Position.Y);
			Log.Message("Body position: {0}", body.Position);
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

