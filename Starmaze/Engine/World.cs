using System;
using System.Collections.Generic;
using System.Linq;
using Starmaze.Engine;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

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


		public event EventHandler<FrameEventArgs> OnUpdate;
		public event EventHandler<KeyboardKeyEventArgs> OnKeyPress;
		public event EventHandler<KeyboardKeyEventArgs> OnKeyRelease;
		public event EventHandler<EventArgs> OnDeath;

		public World(Actor player, WorldMap map, string initialZone, string initialRoom)
		{
			Actors = new HashSet<Actor>();
			ActorsToAdd = new HashSet<Actor>();
			ActorsToRemove = new HashSet<Actor>();
			Space = new Space();

			RenderManager = new RenderManager();

			Map = map;
			ChangeRoom(Map[initialZone][initialRoom]);
			AddActor(player);
		}

		/// <summary>
		/// This essentially clears all objects from the world.
		/// Used when moving from one room to another,
		/// when we switch out all objects for new ones.
		/// </summary>
		public void ClearWorld()
		{
			Space = new Space();
			RenderManager = new RenderManager();

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
		}

		public void Draw(ViewManager view)
		{
			RenderManager.Render(view);
		}

		public void HandleKeyDown(OpenTK.Input.KeyboardKeyEventArgs e)
		{
			// Okay, the 'event handler with nothing attached == null' idiom is a pain in the ass
			if (OnKeyPress != null) {
				OnKeyPress(this, e);
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

		public void HandleKeyUp(OpenTK.Input.KeyboardKeyEventArgs e)
		{
			if (OnKeyRelease != null) {
				OnKeyRelease(this, e);
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

