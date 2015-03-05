using System;
using System.Collections.Generic;
using OpenTK;

namespace Starmaze.Engine
{
	/// <summary>
	/// An Actor is any object that exists in the game world.
	/// </summary>
	public class Actor : IComparable<Actor>
	{
		// Components.
		public List<Component> Components;
		// We handle the Body specially since it's common to want to get it directly
		// But in the end it has to go in the Components set as well(?)
		Body _body;

		[Newtonsoft.Json.JsonIgnore]
		public Body Body {
			get {
				return _body;
			}
			set {
				Components.Remove(_body);
				_body = value;
				Components.Add(_body);
			}
		}

		[System.Runtime.Serialization.OnDeserialized]
		protected void PostDeserialize(System.Runtime.Serialization.StreamingContext context)
		{
			Log.Message("Deserializing actor");
			var body = GetComponent<Body>();
			Body = body;
		}

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

		// Other properties
		readonly long OrderingNumber;
		public string RenderClass;
		public bool Alive = true;
		public bool KeepOnRoomChange = false;
		// Used for StaticRenderer
		//public VertexArray Model;
		public RendererParams RenderParams;
		// XXX: Dependency inversion
		public World World;

		public Actor()
		{
			RenderClass = "TestRenderer";
			OrderingNumber = Util.GetSerial();
			//Model = null;
			Components = new List<Component>();
		}

		/// <summary>
		/// Provides an ordering to Actors, allowing them to be sorted consistently, so that the rendering
		/// code always draws them in the same order.
		/// </summary>
		/// <returns>The to.</returns>
		/// <param name="other">Other Actor.</param>
		public int CompareTo(Actor other)
		{
			return OrderingNumber.CompareTo(other.OrderingNumber);
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
	}
}

