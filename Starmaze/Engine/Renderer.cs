using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	using RendererSet = Dictionary<Renderer, HashSet<Actor>>;

	public class RenderManager
	{
		int ScreenW, ScreenH;
		SortedDictionary<Layer, RendererSet> Renderers;

		public RenderManager(int screenw, int screenh)
		{
			ScreenW = screenw;
			ScreenH = screenh;
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

		public void Render()
		{
			foreach (var layer in Renderers) {
				var renderset = layer.Value;
				foreach (var i in renderset) {
					var renderer = i.Key;
					var actors = i.Value;
					renderer.RenderActors(actors);
				}
			}
		}
	}

	public enum Layer
	{
		BG = 0,
		FG = 1,
		GUI = 2,
	}

	/// <summary>
	/// A Renderer is an object that does the drawing for an Actor.  It's not quite a Component, but
	/// you can treat it a little like one.  But while Component's are attached to Actor's and each
	/// Actor has its own, there's only one Renderer of each type and Actors just all refer to the
	/// same one.
	/// </summary>
	public class Renderer : IComparable<Renderer>
	{
		public Layer Layer;
		public Shader Shader;

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

		public virtual void RenderActor(Actor act)
		{

		}

		public void RenderActors(IEnumerable<Actor> actors)
		{
			RenderStart();
			foreach (var act in actors) {
				RenderActor(act);
			}
			RenderEnd();
		}

		public int CompareTo(Renderer other)
		{
			return Layer.CompareTo(other.Layer);
		}
	}
}

