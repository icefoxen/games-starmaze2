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
		public double AspectRatio;
		public VSyncMode Vsync;
		public GameWindowFlags WindowMode;
		// public string Logfile;

		public GameOptions()
		{
			ResolutionW = 1024;
			ResolutionH = 768;
			Vsync = VSyncMode.On;
			AspectRatio = 4.0 / 3.0;
			WindowMode = GameWindowFlags.Default;
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
		FollowCam Camera;
		GUI Gui;

		public StarmazeWindow(GameOptions options)
			// Using 32 as the color format and depth causes issues on Linux, see
			// https://github.com/opentk/opentk/issues/108
			: base(options.ResolutionW, options.ResolutionH, new GraphicsMode(24, 24, 0, 0), Util.WindowTitle, 
			       options.WindowMode, DisplayDevice.Default,
			       Util.GlMajorVersion, Util.GlMinorVersion, GraphicsContextFlags.Default)
				// Comment out previous and uncomment below to test with OpenGL ES 2.0
			       // 2, 0, GraphicsContextFlags.Embedded)
		{
			// All 'construction' is basically done in OnLoad() because that guarentees the OpenGL context has been
			// set up.
			Options = options;
		}



		WorldMap BuildTestLevel()
		{
			var zone = new Zone("TestZone");
			var actors1 = new Actor[] {
				new BoxBlock(new BBox(-40, -35, 40, -30), Color4.Blue),
				new BoxBlock(new BBox(-40, 30, 40, 35), Color4.Blue),
				new BoxBlock(new BBox(-45, -35, -40, 35), Color4.Blue),
				new BoxBlock(new BBox(40, -35, 45, 35), Color4.Blue),
			};
			var actors2 = new Actor[] {
				new BoxBlock(new BBox(-40, -35, 40, -30), Color4.Yellow),
				//new BoxBlock(new BBox(-40, 30, 40, 35), Color4.Yellow),
				new BoxBlock(new BBox(-45, -35, -40, 35), Color4.Yellow),
				new BoxBlock(new BBox(40, -35, 45, 35), Color4.Yellow),
			};
			var room1 = new Room("TestRoom1", actors1);
			var room2 = new Room("TestRoom2", actors2);
			zone.AddRoom(room1);
			zone.AddRoom(room2);
			var map = new WorldMap();
			map.AddZone(zone);
			return map;
		}

		protected override void OnLoad(System.EventArgs e)
		{
			VSync = Options.Vsync;
			Graphics.Init();
			// Has to be called after the Graphics setup if it's going to be preloading
			// textures and shaders and such...
			Resources.InitResources();
			var map = BuildTestLevel();
			var player = new Player();
			View = new ViewManager(Util.LogicalScreenWidth, Util.LogicalScreenWidth / Options.AspectRatio);
			Camera = new FollowCam(player, Util.LogicalScreenWidth, Util.LogicalScreenWidth / Options.AspectRatio);
			World = new World(player, map, "TestZone", "TestRoom2");
			Gui = new GUI();
			SetupEvents();
		}

		protected override void OnUnload(EventArgs e)
		{
			Resources.CleanupResources();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			GL.Viewport(this.ClientRectangle);

		}

		void SetupEvents()
		{
			// We can just override methods and have the same effect, but doing it like this allows us
			// a layer of indirection that will eventually let us do things like have a 
			// menu screen more easily.
			RenderFrame += HandleRender;
			UpdateFrame += HandleUpdate;
			KeyDown += HandleKeyDown;
			KeyUp += HandleKeyUp;
		}

		void HandleKeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
		{
			if (e.Key == OpenTK.Input.Key.Escape) {
				Exit();
			} else {
				World.HandleKeyDown(e);
			}
		}

		void HandleKeyUp(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
		{
			World.HandleKeyUp(e);
		}

		void HandleUpdate(object sender, FrameEventArgs e)
		{
			World.Update(e);
			Camera.Update(e.Time);
		}

		void HandleRender(object sender, FrameEventArgs e)
		{
			Graphics.ClearScreen();
			View.CenterOn(Camera.CurrentPos);
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
