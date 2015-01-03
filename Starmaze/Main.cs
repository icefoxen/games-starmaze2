using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Starmaze.Engine;

namespace Starmaze
{
	public class World : GameWindow
	{
		RenderManager RenderManager;
		Actor Player;
		Actor Gui;
		ViewManager View;
		HashSet<Actor> Actors;
		HashSet<Actor> ActorsToAdd;
		HashSet<Actor> ActorsToRemove;

		public World(int w, int h)
			// Using 32 as the color format and depth causes issues on Linux, see
			// https://github.com/opentk/opentk/issues/108
			: base(w, h, new GraphicsMode(24, 24, 0, 0), Util.WindowTitle, GameWindowFlags.Default, DisplayDevice.Default,
			       Util.GlMajorVersion, Util.GlMinorVersion, GraphicsContextFlags.Default)
		{
			// All of this is basically done in OnLoad() because that guarentees the OpenGL context has been
			// set up.
		}

		public void AddActor(Actor a)
		{
			ActorsToAdd.Add(a);
		}

		public void RemoveActor(Actor a)
		{
			ActorsToRemove.Add(a);
		}

		void ReallyAddActor(Actor a)
		{
			Actors.Add(a);
			a.World = this;
			RenderManager.AddActorIfPossible(a);
		}

		void ReallyRemoveActor(Actor a)
		{
			Actors.Remove(a);
			RenderManager.RemoveActorIfPossible(a);
		}

		protected override void OnLoad(System.EventArgs e)
		{

			Actors = new HashSet<Actor>();
			ActorsToAdd = new HashSet<Actor>();
			ActorsToRemove = new HashSet<Actor>();
			View = new ViewManager(Width, Height);
			Graphics.InitGL();
			// Probably has to be called after the Graphics setup if it's going to be preloading
			// textures and such...  Bit of a weird dependency inversion there, since the Graphics system
			// will likely require loading shaders and such.
			Starmaze.Engine.Resources.InitResources();
			RenderManager = new RenderManager();

			var testact = new Actor();
			ReallyAddActor(testact);

			// 4/3 aspect ratio...
			// XXX: This should be different.  We're going to need a resolution-independent coordinate
			// system _some_day if we want to make it possible to resize the game, so...
			View = new ViewManager(10f, 7.5f);
		}

		protected override void OnUnload(EventArgs e)
		{
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
		}

		protected override void OnKeyDown(OpenTK.Input.KeyboardKeyEventArgs e)
		{

		}

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			//View.Translate(0.001f, 0.0f);
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			Graphics.StartDraw();
			RenderManager.Render(View);
			Graphics.FinishDraw();
			SwapBuffers();
		}
	}

	public class MainClass
	{
		[STAThread]
		public static void Main()
		{
			const int width = 1024;
			const int height = 768;
			const int physicsRate = 30;
			using (var g = new World(width, height)) {
				Console.WriteLine("Game started...");
				// If no graphics frame rate is entered, it will just run as fast as it can.
				g.Run(physicsRate);
			}
		}
	}
}
