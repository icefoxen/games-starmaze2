using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	public class RenderManager
	{
		// Will be needed eventually for postprocessing...
		//int ScreenW, ScreenH;
		SortedDictionary<Renderer, SortedSet<Actor>> Renderers;

		public RenderManager() //int screenw, int screenh)
		{
			//ScreenW = screenw;
			//ScreenH = screenh;

			// Fill out the required data structures.
			Renderers = new SortedDictionary<Renderer, SortedSet<Actor>>();
			foreach (var renderclass in Util.GetSubclassesOf(typeof(Renderer))) {
				// Remember to get the cached renderer from the resources system here.
				var renderer = Resources.TheResources.GetRenderer(renderclass.Name);
				//Console.WriteLine("Adding renderer {0}", renderer);
				Renderers[renderer] = new SortedSet<Actor>();
				//Renderers.Add(renderer, new SortedSet<Actor>());
			}
			//foreach (var r in Renderers) {
			//	Console.WriteLine("Renderer: {0}, total {1}", r, Renderers.Count);
			//}
		}

		public void Add(Actor act)
		{
			// OPT: Not caching the Renderer instance on the Actor feels a little goofy, but,
			// should work fine if we don't do this too often.
			var renderer = Resources.TheResources.GetRenderer(act.RenderClass);
			//Console.WriteLine("Got renderer {0} for actor {1}", renderer, act);
			Renderers[renderer].Add(act);
			//foreach (var r in Renderers) {
			//	Console.WriteLine("Renderer: {0}, total {1}", r, Renderers.Count);
			//}
		}

		public void Remove(Actor act)
		{
			// OPT: Same as Add()
			var renderer = Resources.TheResources.GetRenderer(act.RenderClass);
			Renderers[renderer].Remove(act);
		}

		public void Render(ViewManager view)
		{
			foreach (var kv in Renderers) {
				var renderer = kv.Key;
				var actors = kv.Value;
				//Console.WriteLine("Rendering {0} with {1}?", actors, renderer);
				renderer.RenderActors(view, actors);
			}
		}
	}

	public enum ZOrder
	{
		BG = 10,
		FG = 20,
		GUI = 30,
	}

	/// <summary>
	/// A Renderer is an object that does the drawing for an Actor.  It's not quite a Component, but
	/// you can treat it a little like one.  But while Component's are attached to Actor's and each
	/// Actor has its own, there's only one Renderer of each type and Actors just all refer to the
	/// same one.  This one just draws nothing.
	/// </summary>
	public abstract class Renderer : IComparable<Renderer>
	{
		public ZOrder ZOrder = ZOrder.FG;
		long Serial;
		protected Shader Shader;

		public Renderer()
		{
			Serial = Util.GetSerial();
		}

		public virtual void RenderStart()
		{
		}

		public virtual void RenderEnd()
		{
		}

		public virtual void RenderActor(ViewManager view, Actor act)
		{

		}

		public virtual void RenderActors(ViewManager view, IEnumerable<Actor> actors)
		{
			RenderStart();
			foreach (var act in actors) {
				RenderActor(view, act);
			}
			RenderEnd();
		}

		/// <Docs>To be added.</Docs>
		/// <para>Returns the sort order of the current instance compared to the specified object.</para>
		/// <summary>
		/// Renderers sort themselves first by their Z order, then in an arbitrary but consistent order.
		/// This way we can all just put them in a SortedDictionary and have them draw in order without
		/// an additional data structure to draw one layer at a time.
		/// </summary>
		/// <returns>The to.</returns>
		/// <param name="other">Other.</param>
		// OPT: I feel like it should be possible to do this without the if.
		public int CompareTo(Renderer other)
		{
			if (other.ZOrder != ZOrder) {
				return ZOrder.CompareTo(other.ZOrder);
			} else {
				return other.Serial.CompareTo(Serial);
			}
		}
	}

	/// <summary>
	/// A renderer that draws nothing.
	/// </summary>
	// OPT: Having a Renderer that draws nothing and using that for invisible objects is wasteful because
	// we still go through all the mechanics of adding and removing Actors to it, calling the code to draw
	// them, and so on.  BUT, for now, it is also simpler, because we don't have to special-case out Renderers
	// that do nothing.  Overriding RenderActors makes life a little better though.
	public class NullRenderer : Renderer
	{
		public override void RenderActors(ViewManager view, IEnumerable<Actor> actors)
		{
		}
	}

	public class TestRenderer : Renderer
	{
		VertexArray Model;

		public TestRenderer()
		{
			//Model = Starmaze.Content.Images.TestModel2();
			//Model = Resources.TheResources.GetModel("TestModel2");
			Model = Resources.TheResources.GetModel("TestModel2");
			Shader = Resources.TheResources.GetShader("default");
		}

		public override void RenderStart()
		{
			Shader.Enable();
		}

		public override void RenderEnd()
		{
			Shader.Disable();
		}

		public override void RenderActor(ViewManager view, Actor act)
		{
			//Console.WriteLine("Drawing actor");
			var pos = new Vector2((float)act.Position.X, (float)act.Position.Y);
			var transform = new Transform(pos, 0.0f);
			var mat = transform.TransformMatrix(view.ProjectionMatrix);
			Shader.UniformMatrix("projection", mat);
			Model.Draw();
		}
	}
}

