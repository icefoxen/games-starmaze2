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
		Shader shader;
		VertexArray verts;

		public World(int w, int h)
			: base(w, h, new GraphicsMode(), Util.WindowTitle, GameWindowFlags.Default, DisplayDevice.Default,
				Util.GlMajorVersion, Util.GlMinorVersion, GraphicsContextFlags.Default)
		{
			Actors = new HashSet<Actor>();
			ActorsToAdd = new HashSet<Actor>();
			ActorsToRemove = new HashSet<Actor>();

			View = new ViewManager(w, h);
			Graphics.InitGL();
			// Probably has to be called after the Graphics setup if it's going to be preloading
			// textures and such...  Bit of a weird dependency inversion there, since the Graphics system
			// will likely require loading shaders and such.
			Starmaze.Engine.Resources.InitResources();


			View = new ViewManager(10, 10);

			var vertexData = new float[] {
				// Verts
				0.0f, 0.5f, 0.0f,
				0.5f, -0.366f, 0.0f,
				-0.5f, -0.366f, 0.0f,
			};
			var colorData = new float[] {
				// Colors
				1.0f, 0.0f, 0.0f, 1.0f,
				0.0f, 1.0f, 0.0f, 1.0f,
				0.0f, 0.0f, 1.0f, 1.0f,
			};

			shader = Resources.TheResources.GetShader("default");

			var v = new VertexAttributeArray[] {
				new VertexAttributeArray("position", vertexData, 3),
				new VertexAttributeArray("color", colorData, 4)
			};
			verts = new VertexArray(shader, v);
			Debug.Assert(false);
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
			RenderManager = new RenderManager(Width, Height);
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
			View.Translate(0.001f, 0.0f);
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			Graphics.StartDraw();
			
			shader.Enable();
			shader.UniformMatrix("projection", View.ProjectionMatrix);
			verts.Draw();
			shader.Disable();

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
