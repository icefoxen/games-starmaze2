using System;
using System.Collections.Generic;
using Starmaze.Engine;
using OpenTK;
using OpenTK.Graphics;

namespace Starmaze.Game
{
	/// <summary>
	/// This is the object that actually *runs* the game, holds on to all the Actors,
	/// handles the various components and subsystems like graphics and rendering, etc.
	/// </summary>
	public class World
	{
		WorldMap Map;
		Room CurrentRoom;
		Player Player;
		RenderManager RenderManager;
		Space Space;
		HashSet<Actor> Actors;
		HashSet<Actor> ActorsToAdd;
		HashSet<Actor> ActorsToRemove;

		public World()
		{

			Actors = new HashSet<Actor>();
			ActorsToAdd = new HashSet<Actor>();
			ActorsToRemove = new HashSet<Actor>();
			Space = new Space();

			RenderManager = new RenderManager();
			Map = new WorldMap();
			CurrentRoom = new Room();

			Player = new Player();
			ImmediateAddActor(Player);

			var testTerrain1 = new BoxBlock(CurrentRoom, new BBox(-50, 50, -35, -30), Color4.Blue);
			ImmediateAddActor(testTerrain1);
			var testTerrain2 = new BoxBlock(CurrentRoom, new BBox(-50, 50, 35, 30), Color4.Blue);
			ImmediateAddActor(testTerrain2);
			var testTerrain3 = new BoxBlock(CurrentRoom, new BBox(-50, -45, -35, 35), Color4.Blue);
			ImmediateAddActor(testTerrain3);
			var testTerrain4 = new BoxBlock(CurrentRoom, new BBox(45, 50, -35, 35), Color4.Blue);
			ImmediateAddActor(testTerrain4);
		}

		public void StartGame(object initialRoom)
		{

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

		public void ChangeRoom(object newRoom)
		{

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
		}

		void ImmediateRemoveActor(Actor a)
		{
			Actors.Remove(a);
			RenderManager.Remove(a);
			Space.Remove(a.Body);
		}
		/*
		void RawAddActor(Actor a)
		{

		}

		void RawRemoveActor(Actor a)
		{

		}
		*/
		public void Update(double dt)
		{
			Space.Update(dt);
			foreach (var act in Actors) {
				act.Update(dt);
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
			// XXX: This cast makes me suspicious.
			var controller = (KeyboardController)Player.Controller;
			controller.KeyDown(e);
		}

		public void HandleKeyUp(OpenTK.Input.KeyboardKeyEventArgs e)
		{
			var controller = (KeyboardController)Player.Controller;
			controller.KeyUp(e);
		}
	}
}

