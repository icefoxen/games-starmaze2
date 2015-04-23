using System;
using System.Collections.Generic;
using OpenTK;

namespace Starmaze.Engine
{
	/// <summary>
	/// An Actor is any object that exists in the game world.
	/// </summary>
	public class Actor
	{
		// Components.
		public List<Component> _components;

		public List<Component> Components { get { return _components; } }

		FBody _body;

		/// <summary>
		/// Essentially a cache.  Returns the Actor's Body component, or null if none.
		/// Automatically set if AddComponent is used to add a Body, or cleared if it is
		/// removed with RemoveComponent.
		/// </summary>
		/// <value>The body.</value>
		public FBody Body {
			get {
				return _body;
			}
		}

		RenderState _renderState;

		/// <summary>
		/// Like Body, this returns the Actor's RenderState component, or null if none.
		/// Automatically set if AddComponent is used to add a RenderState, or cleared if it is
		/// removed with RemoveComponent.
		/// </summary>
		/// <value>The state of the render.</value>
		public RenderState RenderState { 
			get {
				return _renderState;
			} 
		}
		// Other properties
		public bool Alive = true;
		public bool KeepOnRoomChange = false;
		public World World;

		public Actor()
		{
			_components = new List<Component>();
		}

		public virtual void OnUpdate(object sender, EventArgs e)
		{

		}

		/// <summary>
		/// Returns a component of a particular type from the actor.
		/// Note that this really doesn't deal with having multiple components of the same type in the actor.
		/// If no component, returns default.
		/// </summary>
		/// <returns>The component.</returns>
		/// <typeparam name="T">Component type.</typeparam>
		public T GetComponent<T>() where T : Component
		{
			foreach (var c in Components) {
				T test = c as T;
				if (test != null) {
					return test;
				}
			}
			Log.Warn(true, "Could not find component {0} in actor, returning default", typeof(T));
			return default(T);
		}

		public void AddComponent(Component c)
		{
			c.Owner = this;
			if (c is Starmaze.Engine.FBody) {
				_body = c as FBody;
			} else if (c is RenderState) {
				_renderState = c as RenderState;
			}
			Components.Add(c);
		}

		public void RemoveComponent(Component c)
		{
			c.Owner = null;
			if (c is Starmaze.Engine.FBody) {
				_body = null;
			} else if (c is RenderState) {
				_renderState = null;
			}
			Components.Remove(c);
		}

		public void RegisterEvents(World w)
		{
			//Log.Message("{0} registering events", this);
			foreach (var c in Components) {
				c.RegisterEvents(w);
			}
		}

		public void UnregisterEvents(World w)
		{
			foreach (var c in Components) {
				c.UnregisterEvents(w);
			}
		}

		public override string ToString()
		{
			var cstring = String.Join(", ", Components);
			return string.Format("Actor({0})", cstring);
		}
	}
}

