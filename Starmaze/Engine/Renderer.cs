using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	using RendererSet = Dictionary<Renderer, HashSet<Actor>>;

	public class RenderManager
	{
		// Will be needed eventually for postprocessing...
		//int ScreenW, ScreenH;
		SortedDictionary<Layer, RendererSet> Renderers;

		public RenderManager() //int screenw, int screenh)
		{
			//ScreenW = screenw;
			//ScreenH = screenh;
			Renderers = new SortedDictionary<Layer, RendererSet>();
			foreach (Layer layer in Enum.GetValues(typeof(Layer))) {
				Renderers[layer] = new Dictionary<Renderer, HashSet<Actor>>();
			}
			
		}

		public void Add(Renderer renderer, Actor act)
		{
			var layer = Renderers[renderer.Layer];
			// XXX: This is a little annoying; we should be able to add all the renderers and just
			// run with it.
			if (!layer.ContainsKey(renderer)) {
				layer[renderer] = new HashSet<Actor>();
			}
			layer[renderer].Add(act);
		}

		public void AddActorIfPossible(Actor act)
		{
			if (act.Renderer != null) {
				Add(act.Renderer, act);
			}
		}

		public void Remove(Renderer renderer, Actor act)
		{
			var layer = Renderers[renderer.Layer];
			layer[renderer].Remove(act);
		}

		public void RemoveActorIfPossible(Actor act)
		{
			if (act.Renderer != null) {
				Remove(act.Renderer, act);
			}
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
	/// same one.
	/// </summary>
	public class Renderer : IComparable<Renderer>
	{
		public Layer Layer = Layer.FG;
		protected Shader Shader;

		public Renderer()
		{

		}

		public virtual void RenderStart()
		{
			GL.PushAttrib(AttribMask.ColorBufferBit);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			Shader.Enable();
		}

		public virtual void RenderEnd()
		{
			Shader.Disable();
			GL.PopAttrib();
		}

		public virtual void RenderActor(ViewManager view, Actor act)
		{

		}

		public void RenderActors(ViewManager view, IEnumerable<Actor> actors)
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

	public class TestRenderer : Renderer
	{
		VertexArray Model;

		public TestRenderer()
		{
			var vertexData = new float[] {
				// Verts
				-0.5f, -0.5f,   0.0f,
				-0.5f,  0.5f,   0.0f,
				+0.5f,  0.5f,   0.0f,

				+0.5f,  0.5f,   0.0f,
				+0.5f, -0.5f,   0.0f,
				-0.5f, -0.5f,   0.0f,
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

