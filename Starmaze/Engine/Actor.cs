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
		public List<Component> Components { get; set; }

		FBody _body;
		// XXX: This is dangerous 'cause it doesn't handle worlds properly.
		public FBody Body {
			get {
				return _body;
			}
			set {
				Components.Remove(_body);
				_body = value;
				Components.Add(_body);
			}
		}
		// Same with render state
		RenderState _renderState;

		public RenderState RenderState { 
			get {
				return _renderState;
			} 
			set {
				Components.Remove(_renderState);
				_renderState = value;
				Components.Add(_renderState);
			}
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
		// BUGGO: Might need some kind of callback to let the World know that a new component exists
		// and thus needs to be notified of events?  OR, this just can't be called after the actor
		// has been added to the World.  Either way!  It's basically here for purposes of deserialization.
		public void AddComponent(Component c)
		{
			c.Owner = this;
			if (c is Starmaze.Engine.FBody) {
				Body = c as FBody;
			} else if (c is RenderState) {
				RenderState = c as RenderState;
			} else {
				Components.Add(c);
			}
		}
		// Other properties
		public bool Alive = true;
		public bool KeepOnRoomChange = false;
		public World World;

		public Actor()
		{
			Components = new List<Component>();
		}

		public virtual void OnUpdate(object sender, EventArgs e)
		{

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

