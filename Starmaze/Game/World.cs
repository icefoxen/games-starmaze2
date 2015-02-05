using System;
using System.Collections.Generic;
using Starmaze.Engine;
using OpenTK;
using OpenTK.Graphics;
using Newtonsoft.Json;

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

		public ParticleGroup group;
		ParticleRenderer renderer;
		ParticleEmitter emitter;
		ParticleController controller;

		// Events...
		public event EventHandler<EventArgs> OnUpdate;
		public event EventHandler<OpenTK.Input.KeyboardKeyEventArgs> OnKeyPress;
		public event EventHandler<OpenTK.Input.KeyboardKeyEventArgs> OnKeyRelease;
		public event EventHandler<EventArgs> OnDeath;

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

			JsonSerializerSettings jset = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects };

			string json = "";
			foreach (Component c in Player.Components) {
				Log.Message( " Component: {0}",c.ToString());
				json = JsonConvert.SerializeObject(Player,jset);
				//json = JsonConvert.SerializeObject(Player, );
				Log.Message("{0}\n",json);
			}
			Log.Message("Compontents done\n",json);




			string playerJSON = Newtonsoft.Json.JsonConvert.SerializeObject(Player,jset);
			Log.Message("playerJSON: {0}",playerJSON);

			var testTerrain1 = new BoxBlock(CurrentRoom, new BBox(-40, -35, 40, -30), Color4.Blue);
			string terrainJson = Newtonsoft.Json.JsonConvert.SerializeObject(testTerrain1,jset);
			Log.Message("terrainJSON: {0}",terrainJson);
			var test1b = Newtonsoft.Json.JsonConvert.DeserializeObject<BoxBlock>(terrainJson);
			ImmediateAddActor(testTerrain1);
			//ImmediateAddActor(test1b);
			var testTerrain2 = new BoxBlock(CurrentRoom, new BBox(-40, 30, 40, 35), Color4.Blue);
			ImmediateAddActor(testTerrain2);
			var testTerrain3 = new BoxBlock(CurrentRoom, new BBox(-45, -35, -40, 35), Color4.Blue);
			ImmediateAddActor(testTerrain3);
			var testTerrain4 = new BoxBlock(CurrentRoom, new BBox(40, -35, 45, 35), Color4.Blue);
			ImmediateAddActor(testTerrain4);

			group = new ParticleGroup();
			renderer = new ParticleRenderer();
			emitter = new ParticleEmitter();
			controller = new ParticleController();
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
			a.RegisterEvents(this);
		}

		void ImmediateRemoveActor(Actor a)
		{
			Actors.Remove(a);
			RenderManager.Remove(a);
			Space.Remove(a.Body);
			a.UnregisterEvents(this);
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
			// If nothing is listening for an event it will be null
			if (OnUpdate != null) {
				OnUpdate(this, EventArgs.Empty);
			}
			foreach (var act in ActorsToAdd) {
				ImmediateAddActor(act);
			}
			foreach (var act in ActorsToRemove) {
				ImmediateRemoveActor(act);
			}

			controller.Update(dt, group);
			emitter.Update(dt, group);
			ActorsToAdd.Clear();
			ActorsToRemove.Clear();
		}

		public void Draw(ViewManager view)
		{
			RenderManager.Render(view);
			renderer.Draw(view, group);
		}

		public void HandleKeyDown(OpenTK.Input.KeyboardKeyEventArgs e)
		{
			// Okay, the 'event handler with nothing attached == null' idiom is a pain in the ass
			if (OnKeyPress != null) {
				OnKeyPress(this, e);
			} else {
				Log.Message("Thing?");
			}
		}

		public void HandleKeyUp(OpenTK.Input.KeyboardKeyEventArgs e)
		{
			if (OnKeyRelease != null) {
				OnKeyRelease(this, e);
			}
		}

		// Hrmbl grmbl C# blrgl not letting random classes invoke events
		public void TriggerOnDeath(object sender, EventArgs e)
		{
			if (OnDeath != null) {
				OnDeath(sender, e);
			}
		}
	}
}

