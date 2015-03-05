using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	/*
	 * This could potentially work but suddenly we have a million different types of actors...
	 * Because suddenly we have Actor<T> which cointains slot T RendererParams.
	 * But this DOES bind together the Renderer and RendererParams so both are always the right type...
	 * But any collection of Actor's suddenly becomes a collection of Actor<T>, and we have to implement
	 * IActor to abstract that out some...  Doable, but weird.
	public class RendererParams<T> where T : Renderer
	{
		T Renderer;

		public RendererParams(T renderer)
		{
			Renderer = renderer;
		}
	}

	public class StaticRendererParams : RendererParams<StaticRenderer>
	{
		public VertexArray Model;

		public StaticRendererParams(StaticRenderer renderer, VertexArray model) : base(renderer)
		{
			Log.Assert(model != null);
			Model = model;
		}
	}
	*/
	public class RendererParams
	{
		public readonly string RenderClass;

		public RendererParams(string renderclass)
		{
			RenderClass = renderclass;
		}
	}

	public class StaticRendererParams : RendererParams
	{
		public VertexArray Model;

		public StaticRendererParams(VertexArray model) : base("StaticRenderer")
		{
			Log.Assert(model != null);
			Model = model;
		}
	}

	/// <summary>
	/// A class to manage drawing a heterogenous set of actors.
	/// This class keeps track of all Renderers and all Actors, and holds the association
	/// between one and the other.  It also preloads Renderers and tracks; all an Actor has to do is
	/// specify a RenderClass string.  It also handles Z ordering in the Renderers and the postprocessing
	/// pipeline.
	/// </summary>
	public class RenderManager
	{
		SortedDictionary<IRenderer, SortedSet<Actor>> Renderers;
		PostprocPipeline postproc;

		public RenderManager(int width, int height)
		{

			// Fill out the required data structures.
			Renderers = new SortedDictionary<IRenderer, SortedSet<Actor>>();
			foreach (var renderclass in Util.GetSubclassesOf(typeof(Renderer))) {
				// Remember to get the cached renderer from the resources system here.
				var renderer = Resources.TheResources.GetRenderer(renderclass.Name);
				//Console.WriteLine("Adding renderer {0}", renderer);
				Renderers[renderer] = new SortedSet<Actor>();
				//Renderers.Add(renderer, new SortedSet<Actor>());
			}

			postproc = new PostprocPipeline(width, height);
			var ppShader = Resources.TheResources.GetShader("postproc");
			postproc.AddStep(ppShader);
			//postproc.AddStep(new GlowFilter(width, width));
			var fxaaShader = Resources.TheResources.GetShader("fxaa");
			postproc.AddStep(fxaaShader);
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

			postproc.RenderWith(thunk);
		}

		public void Resize(int width, int height)
		{
			postproc.Resize(width, height);
		}
	}

	public enum ZOrder
	{
		BG = 10,
		FG = 20,
		GUI = 30,
	}

	public interface IRenderer : IComparable<IRenderer>
	{

		ZOrder zOrder { get; set; }

		long serial { get; set; }

		void RenderStart();

		void RenderMany(ViewManager view, IEnumerable<Actor> actors);
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
	public abstract class Renderer : IComparable<IRenderer>, IRenderer
	{
		public ZOrder zOrder { get; set; }

		public long serial { get; set; }

		protected GLDiscipline discipline;
		protected Shader shader;

		public Renderer()
		{
			zOrder = ZOrder.FG;
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
		public int CompareTo(IRenderer other)
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
			model = Resources.TheResources.GetModel("TestModel");
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

		public override void RenderOne(ViewManager view, Actor act)
		{
			var pos = new Vector2((float)act.Body.Position.X, (float)act.Body.Position.Y);
			var transform = new Transform(pos, 0.0f);
			var mat = transform.TransformMatrix(view.ProjectionMatrix);
			shader.UniformMatrix("projection", mat);
			var parms = act.RenderParams as StaticRendererParams;
			if (parms != null) {
				parms.Model.Draw();
			}
		}
	}

	public class TexTestRenderer : Renderer
	{
		Texture tex;
		VertexArray billboard;

		public TexTestRenderer() : base()
		{
			shader = Resources.TheResources.GetShader("default-tex");
			discipline = GLDiscipline.DEFAULT;
			tex = Resources.TheResources.GetTexture("playertest");
			billboard = Resources.TheResources.GetModel("Billboard");
		
		}

		public override void RenderOne(ViewManager view, Actor act)
		{
			var pos = new Vector2((float)act.Body.Position.X, (float)act.Body.Position.Y);
			var transform = new Transform(pos, 0.0f);
			var mat = transform.TransformMatrix(view.ProjectionMatrix);
			shader.UniformMatrix("projection", mat);
			// This is, inconveniently, not the texture handle but in fact the texture unit offset.
			shader.Uniformi("texture", 0);
			tex.Enable();
			billboard.Draw();
			tex.Disable();
		}
	}

	public class SpriteTestRenderer : Renderer
	{
		VertexArray billboard;

		public SpriteTestRenderer() : base()
		{
			shader = Resources.TheResources.GetShader("default-sprite");
			discipline = GLDiscipline.DEFAULT;
			billboard = Resources.TheResources.GetModel("Billboard");

		}

		public override void RenderOne(ViewManager view, Actor act)
		{
			var sprite = act.GetComponent<Sprite>();
			var pos = new Vector2((float)act.Body.Position.X, (float)act.Body.Position.Y);
			var transform = new Transform(pos, 0.0f, new Vector2(5, 5));
			var mat = transform.TransformMatrix(view.ProjectionMatrix);
			shader.UniformMatrix("projection", mat);
			shader.Uniformi("texture", 0);
			var coords = sprite.GetBox();
			shader.Uniformf("atlasCoords", coords.X, coords.Y, coords.Z, coords.W);
			sprite.Atlas.Enable();
			billboard.Draw();
			sprite.Atlas.Disable();
		}
	}

	public class SwirlyTestRenderer : Renderer
	{
		readonly Color4[] Colors = new Color4[] {
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
		};
		const double startSize = 50;
		const double scaleFactor = 0.9;
		List<VertexArray> Rects;
		double time;

		public SwirlyTestRenderer() : base()
		{
			shader = Resources.TheResources.GetShader("default");
			discipline = GLDiscipline.DEFAULT;

			Rects = new List<VertexArray>();
			var size = startSize;
			foreach (var color in Colors) {
				var rect = Starmaze.Content.Images.FilledRectCenter(0, 0, size, size, color);
				Rects.Add(rect);
				size *= scaleFactor;
			}

			time = 0.0;
		}

		public override void RenderOne(ViewManager view, Actor act)
		{
			/*
			var pos = new Vector2((float)act.Body.Position.X, (float)act.Body.Position.Y);
			var transform = new Transform(pos, 0.0f);
			var mat = transform.TransformMatrix(view.ProjectionMatrix);
			shader.UniformMatrix("projection", mat);
			var parms = act.RenderParams as StaticRendererParams;
			if (parms != null) {
				parms.Model.Draw();
			}
			*/


			time += 0.0005;
			for (int i = 0; i < Rects.Count; i++) {
				var pos = Vector2.Zero;
				var rot = (float)Math.Sin((time * i));
				var transform = new Transform(pos, rot);
				var mat = transform.TransformMatrix(view.ProjectionMatrix);
				shader.UniformMatrix("projection", mat);
				Rects[i].Draw();
			}

		}
	}
}

