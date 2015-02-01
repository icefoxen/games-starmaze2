using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	/// <summary>
	/// A class to manage drawing a heterogenous set of actors.
	/// This class keeps track of all Renderers and all Actors, and holds the association
	/// between one and the other.  It also preloads Renderers and tracks; all an Actor has to do is
	/// specify a RenderClass string.  It also handles Z ordering in the Renderers.
	/// </summary>
	public class RenderManager
	{
		// Will be needed eventually for postprocessing...
		//int ScreenW, ScreenH;
		SortedDictionary<Renderer, SortedSet<Actor>> Renderers;

		PostprocPipeline postproc;

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

			postproc = new PostprocPipeline();
			var ppShader = Resources.TheResources.GetShader("postproc");
			postproc.AddStep(ppShader);
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
			Action thunk = () => {
				foreach (var kv in Renderers) {
					var renderer = kv.Key;
					var actors = kv.Value;
					//Console.WriteLine("Rendering {0} with {1}?", actors, renderer);
					renderer.RenderMany(view, actors);
				}
			};

			thunk();
			//postproc.RenderWith(thunk);
		}
	}

	public enum ZOrder
	{
		BG = 10,
		FG = 20,
		GUI = 30,
	}

	/// <summary>
	/// A Renderer is an object that does the drawing for a specific type of Actor.
	/// This is an abstract base class; all children from this will be automatically
	/// added to the list of available Renderer's.  Each Renderer exists precisely once,
	/// and is instantiated at load time, so any resources it creates (such as loading meshes
	/// or images) and holds on to are loaded only once and are disposed of properly.
	/// 
	/// Note that these should not keep Actor-level state around.  If it needs some data per-Actor,
	/// the Actors need to be able to provide it.
	/// </summary>
	public abstract class Renderer : IComparable<Renderer>
	{
		ZOrder zOrder = ZOrder.FG;
		protected long serial;
		protected GLDiscipline discipline;
		protected Shader shader;

		public Renderer()
		{
			serial = Util.GetSerial();
		}

		public virtual void RenderStart()
		{
			Graphics.TheGLTracking.SetDiscipline(discipline);
			Graphics.TheGLTracking.SetShader(shader);
		}

		/// <summary>
		/// Must be overriden in subclasses.
		/// </summary>
		/// <param name="view">View.</param>
		/// <param name="act">Act.</param>
		public virtual void RenderOne(ViewManager view, Actor act)
		{

		}

		public virtual void RenderMany(ViewManager view, IEnumerable<Actor> actors)
		{
			RenderStart();
			foreach (var act in actors) {
				RenderOne(view, act);
			}
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
			if (other.zOrder != zOrder) {
				return zOrder.CompareTo(other.zOrder);
			} else {
				return other.serial.CompareTo(serial);
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
		public override void RenderMany(ViewManager view, IEnumerable<Actor> actors)
		{
		}
	}

	public class TestRenderer : Renderer
	{
		VertexArray model;

		public TestRenderer() : base()
		{
			//Model = Starmaze.Content.Images.TestModel2();
			//Model = Resources.TheResources.GetModel("TestModel2");
			model = Resources.TheResources.GetModel("TestModel2");
			shader = Resources.TheResources.GetShader("default");
			discipline = GLDiscipline.DEFAULT;
		}

		public override void RenderOne(ViewManager view, Actor act)
		{
			//Console.WriteLine("Drawing actor");
			var pos = new Vector2((float)act.Body.Position.X, (float)act.Body.Position.Y);
			var transform = new Transform(pos, 0.0f);
			var mat = transform.TransformMatrix(view.ProjectionMatrix);
			shader.UniformMatrix("projection", mat);
			model.Draw();
		}
	}

	public class StaticRenderer : Renderer
	{
		public StaticRenderer() : base()
		{
			shader = Resources.TheResources.GetShader("default");
			discipline = GLDiscipline.DEFAULT;
		}
		// XXX: This loads more optional properties onto Actors, in terms of the
		// Model property.  Not sure if it's a good idea.
		public override void RenderOne(ViewManager view, Actor act)
		{
			var pos = new Vector2((float)act.Body.Position.X, (float)act.Body.Position.Y);
			var transform = new Transform(pos, 0.0f);
			var mat = transform.TransformMatrix(view.ProjectionMatrix);
			shader.UniformMatrix("projection", mat);
			if (act.Model != null) {
				act.Model.Draw();
			}
		}
	}
}

