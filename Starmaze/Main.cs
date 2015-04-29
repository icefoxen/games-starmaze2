using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using Starmaze.Engine;
using Starmaze.Game;

namespace Starmaze
{
	/// <summary>
	/// Class that does all the setup, teardown, and actually makes the window
	/// and stuff like that.
	/// </summary>
	public class StarmazeWindow : GameWindow
	{
		public GameOptions Options;
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

		protected override void OnLoad(EventArgs e)
		{
			VSync = Options.Vsync;
			Graphics.Init();
			// Has to be called after the Graphics setup if it's going to be preloading
			// textures and shaders and such...
			Resources.Init(Options);
			var map = BuildTestLevel();
			var actCfg = Resources.TheResources.GetJson("player");
			var player = SaveLoad.Load<Actor>(actCfg);
			//player.Body.AddGeom(new BoxGeom(new BBox(-5, -15, 5, 5)));
			View = new ViewManager(Util.LogicalScreenWidth, Util.LogicalScreenWidth / Options.AspectRatio);
			Camera = new FollowCam(player, Util.LogicalScreenWidth, Util.LogicalScreenWidth / Options.AspectRatio);
			World = new World(player, map, "TestZone", "TestRoom1");
			Gui = new GUI(Util.LogicalScreenWidth, Util.LogicalScreenWidth / Options.AspectRatio);
			//Gui.CreateGUIText(World, new Vector2d(-55, 70), "FPS: 00");
			SetupEvents();

            ParticleComponent p_component = new ParticleComponent(World,10f);
            p_component.setupEmitter(new LineEmitter(Color4.Red,50f,90,10f,0.1,2f));
            player.AddComponent(p_component);

			fpsTimer.Start();
		}

		protected override void OnUnload(EventArgs e)
		{
			Resources.CleanupResources();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			GL.Viewport(this.ClientRectangle);
			World.Resize(Width, Height);
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
			if (e.Key == OpenTK.Input.Key.Escape ||
			    (e.Key == OpenTK.Input.Key.F4 && e.Alt)) {
				Exit();
			} else if (!e.IsRepeat) {
				var keyaction = Options.KeyBinding.Action(e.Key);
				if (keyaction != InputAction.Unbound) {
					World.HandleKeyDown(keyaction);
				}
			}
		}

		void HandleKeyUp(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
		{
			var keyaction = Options.KeyBinding.Action(e.Key);
			if (keyaction != InputAction.Unbound) {
				World.HandleKeyUp(keyaction);
			}
		}
		// XXX: This FPS counter is a little hacky, make it better.
		Stopwatch fpsTimer = new Stopwatch();
		const double fpsInterval = 5;
		int frames = 0;

		void HandleUpdate(object sender, FrameEventArgs e)
		{
			World.Update(e);
			Camera.Update(e.Time);
            
			//Gui.DrawString("TESTING",new Vector2d(0,-80));
          
			if (fpsTimer.ElapsedMilliseconds > (fpsInterval * 1000)) {
				fpsTimer.Restart();
				Log.Message("FPS: {0}", frames / fpsInterval);
				Gui.editGUIText("" + -55 + "" + 70, String.Format("FPS: {0:00}", (frames / fpsInterval)));
				frames = 0;
			}
		}

		void HandleRender(object sender, FrameEventArgs e)
		{
			Graphics.ClearScreen();
			View.CenterOn(Camera.CurrentPos);
			World.Draw(View);
          
			Gui.Draw(Camera.CurrentPos);           
			
			SwapBuffers();
			frames += 1;
		}
	}

	public class MainClass
	{
		[STAThread]
		public static void Main()
		{
			// This is set up first thing so that GameOptions can use it.
			Log.Init();
			//GameOptions o = new GameOptions();
			GameOptions o = GameOptions.OptionsFromFile();
			// Save game options so that if there is no options file we create one.
			GameOptions.OptionsToFile(o);
			var physicsRate = Physics.PHYSICS_HZ;
			using (var g = new StarmazeWindow(o)) {
				Log.Message("Starting game...");
				// If no graphics frame rate is entered, it will just run as fast as it can.
				g.Run(physicsRate);
			}
		}
	}
}
