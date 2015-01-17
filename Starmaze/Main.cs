using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Starmaze.Engine;
using Starmaze.Game;

namespace Starmaze
{
	public class GameOptions
	{
		public int ResolutionW;
		public int ResolutionH;
		public VSyncMode Vsync;
		public double? FpsLimit;
		// public string Logfile;

		public GameOptions()
		{
			ResolutionW = 1024;
			ResolutionH = 768;
			Vsync = VSyncMode.On;
			FpsLimit = null;
		}
	}

	/// <summary>
	/// Class that does all the setup, teardown, and actually makes the window
	/// and stuff like that.
	/// </summary>
	public class StarmazeWindow : GameWindow
	{
		GameOptions Options;
		World World;
		ViewManager View;
		GUI Gui;

		public StarmazeWindow(GameOptions options)
			// Using 32 as the color format and depth causes issues on Linux, see
			// https://github.com/opentk/opentk/issues/108
			: base(options.ResolutionW, options.ResolutionH, new GraphicsMode(24, 24, 0, 0), Util.WindowTitle, 
			       GameWindowFlags.Default, DisplayDevice.Default,
			       Util.GlMajorVersion, Util.GlMinorVersion, GraphicsContextFlags.Default)
		{
			// All 'construction' is basically done in OnLoad() because that guarentees the OpenGL context has been
			// set up.
			Options = options;
		}

		protected override void OnLoad(System.EventArgs e)
		{
			VSync = Options.Vsync;
			// BUGGO: Aspect ratio, also, magic constants.
			// Also, should this be part of the Window or World class?  It's basically low-level drawing, so...
			View = new ViewManager(120, 80);
			Graphics.Init();
			// Has to be called after the Graphics setup if it's going to be preloading
			// textures and shaders and such...
			Resources.InitResources();
			World = new World();
			Gui = new GUI();
			SetupEvents();
			// TODO: Generate world, set up initial room and camera if needed.
		}

		protected override void OnUnload(EventArgs e)
		{
			Resources.CleanupResources();
		}

		// TODO: Handle or disallow resizing.
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
		}

		void SetupEvents()
		{
			// We can just override methods and have the same effect, but doing it like this allows us
			// a layer of indirection that will eventually let us do things like have a 
			// menu screen more easily.
			RenderFrame += new EventHandler<FrameEventArgs>(this.HandleRender);
			UpdateFrame += new EventHandler<FrameEventArgs>(this.HandleUpdate);
			KeyDown += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(HandleKeyDown);
			KeyUp += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(HandleKeyUp);
		}

		void HandleKeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
		{
			World.HandleKeyPress(e);
		}

		void HandleKeyUp(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
		{
			World.HandleKeyRelease(e);
		}

		void HandleUpdate(object sender, FrameEventArgs e)
		{
			World.Update(e.Time);
		}

		void HandleRender(object sender, FrameEventArgs e)
		{
			Graphics.ClearScreen();
			World.Draw(View);
			Gui.Draw();
			SwapBuffers();
		}
	}

	public class MainClass
	{
		[STAThread]
		public static void Main()
		{
			GameOptions o = new GameOptions();
			var physicsRate = Physics.PHYSICS_HZ;
			using (var g = new StarmazeWindow(o)) {
				Console.WriteLine("Game started...");
				// If no graphics frame rate is entered, it will just run as fast as it can.
				g.Run(physicsRate);
			}
		}
	}
}
