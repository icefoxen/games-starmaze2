using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	using RendererSet = Dictionary<Renderer, SortedSet<Actor>>;

	public class RenderManager
	{
		// Will be needed eventually for postprocessing...
		//int ScreenW, ScreenH;
		SortedDictionary<Layer, RendererSet> Renderers;

		public RenderManager() //int screenw, int screenh)
		{
			//ScreenW = screenw;
			//ScreenH = screenh;

			// Fill out the required data structures.
			Renderers = new SortedDictionary<Layer, RendererSet>();
			foreach (Layer layer in Enum.GetValues(typeof(Layer))) {
				Renderers[layer] = new Dictionary<Renderer, SortedSet<Actor>>();
			}
			foreach (var renderclass in Util.GetSubclassesOf(typeof(Renderer))) {
				// Remember to get the cached renderer from the resources system here.
				var renderer = Resources.TheResources.GetRenderer(renderclass.Name);
				var layer = renderer.Layer;
				Renderers[layer][renderer] = new SortedSet<Actor>();
			}
		}

		public void Add(Renderer renderer, Actor act)
		{
			var layer = Renderers[renderer.Layer];
			layer[renderer].Add(act);
		}

		public void AddActorIfPossible(Actor act)
		{
			Add(act.Renderer, act);
		}

		public void Remove(Renderer renderer, Actor act)
		{
			var layer = Renderers[renderer.Layer];
			layer[renderer].Remove(act);
		}

		public void RemoveActorIfPossible(Actor act)
		{
			Remove(act.Renderer, act);
		}

		public void Render(ViewManager view)
		{
			foreach (var layer in Renderers) {
				var renderset = layer.Value;
				foreach (var i in renderset) {
					var renderer = i.Key;
					var actors = i.Value;
					renderer.RenderActors(view, actors);
				}
			}
		}
	}

	public enum Layer
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
		public Layer Layer = Layer.FG;
		protected Shader Shader;

		public Renderer()
		{

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

		public int CompareTo(Renderer other)
		{
			return Layer.CompareTo(other.Layer);
		}
	}

	/// <summary>
	/// A renderer that draws nothing.
	/// </summary>
	// XXX: Having a Renderer that draws nothing and using that for invisible objects is wasteful because
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
			var vertexData = new float[] {
				// Verts
				-0.5f, -0.5f, 0.0f,
				-0.5f, 0.5f, 0.0f,
				+0.5f, 0.5f, 0.0f,

				+0.5f, 0.5f, 0.0f,
				+0.5f, -0.5f, 0.0f,
				-0.5f, -0.5f, 0.0f,
			};
			var colorData = new float[] {
				// Colors
				1.0f, 0.0f, 0.0f, 1.0f,
				0.0f, 1.0f, 0.0f, 1.0f,
				0.0f, 0.0f, 1.0f, 1.0f,

				0.0f, 0.0f, 1.0f, 1.0f,
				0.0f, 1.0f, 0.0f, 1.0f,
				1.0f, 0.0f, 0.0f, 1.0f,
			};

			Shader = Resources.TheResources.GetShader("default");

			var v = new VertexAttributeArray[] {
				new VertexAttributeArray("position", vertexData, 3),
				new VertexAttributeArray("color", colorData, 4)
			};
			Model = new VertexArray(Shader, v, prim: PrimitiveType.Triangles);
		}

		public override void RenderStart()
		{
			Shader.Enable();
		}

		public override void RenderEnd()
		{
			Shader.Disable();
		}

		float rot = 0.0f;

		public override void RenderActor(ViewManager view, Actor act)
		{
			rot += 0.01f;
			var transform = new Transform(new Vector2(rot, 0), rot, new Vector2(1.0f, rot));
			var mat = transform.TransformMatrix(view.ProjectionMatrix);
			//var transform = Matrix4.CreateTranslation(0, rot, 0);
			//var mat = view.ProjectionMatrix * transform;
			Shader.UniformMatrix("projection", mat);
			Model.Draw();
		}
	}
}

