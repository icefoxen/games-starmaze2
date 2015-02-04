using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using OpenTK;

namespace Starmaze.Engine
{
	[Flags]
	public enum EventType
	{
		None = 0,
		OnUpdate = 1 << 0,
		OnKeyPress = 1 << 1,
		OnKeyRelease = 1 << 2,
		OnDeath = 1 << 3,
	}

	/// <summary>
	/// Base class for all Components.  Actors are made of Components.
	/// </summary>
	public class Component
	{
		//TODO: XXX seralization bandaid must fix XXX
		[JsonIgnore]
		public Actor Owner;
		public EventType HandledEvents;

		public Component(Actor owner)
		{
			Owner = owner;
			HandledEvents = EventType.None;
		}

		// It would be nice if these were unnecessary; we could just provide a list of events somehow
		// that each component cares about and the system would be smart enough to add/remove them itself.
		public void RegisterEvents(Starmaze.Game.World w)
		{
			Console.WriteLine("{0} registering events", this);
			if (HandledEvents.HasFlag(EventType.OnUpdate)) {
				w.OnUpdate += OnUpdate;
			}
			if (HandledEvents.HasFlag(EventType.OnKeyPress)) {
				w.OnKeyPress += OnKeyPress;
			}
			if (HandledEvents.HasFlag(EventType.OnKeyRelease)) {
				w.OnKeyRelease += OnKeyRelease;
			}
			if (HandledEvents.HasFlag(EventType.OnDeath)) {
				w.OnDeath += OnDeath;
			}
		}

		public void UnregisterEvents(Starmaze.Game.World w)
		{
			if (HandledEvents.HasFlag(EventType.OnUpdate)) {
				w.OnUpdate -= OnUpdate;
			}
			if (HandledEvents.HasFlag(EventType.OnKeyPress)) {
				w.OnKeyPress -= OnKeyPress;
			}
			if (HandledEvents.HasFlag(EventType.OnKeyRelease)) {
				w.OnKeyRelease -= OnKeyRelease;
			}
			if (HandledEvents.HasFlag(EventType.OnDeath)) {
				w.OnDeath -= OnDeath;
			}
		}

		public virtual void OnUpdate(object sender, EventArgs args)
		{

		}

		public virtual void OnKeyPress(object sender, OpenTK.Input.KeyboardKeyEventArgs args)
		{

		}

		public virtual void OnKeyRelease(object sender, OpenTK.Input.KeyboardKeyEventArgs args)
		{

		}

		public virtual void OnDeath(object sender, EventArgs args)
		{

		}
	}

	/// <summary>
	/// An Actor is any object that exists in the game world.
	/// </summary>
	public class Actor : IComparable<Actor>
	{
		// Components.
		public HashSet<Component> Components;
		// We handle the Body specially since it's common to want to get it directly
		// But in the end it has to go in the Components set as well(?)
		Body _body;

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
		// Other properties
		readonly long OrderingNumber;
		public string RenderClass;
		public bool Alive = true;
		public bool KeepOnRoomChange = false;
		// Used for StaticRenderer
		public VertexArray Model;
		// XXX: Dependency inversion
		public Starmaze.Game.World World;

		public Actor()
		{
			RenderClass = "TestRenderer";
			OrderingNumber = Util.GetSerial();
			Model = null;
			Components = new HashSet<Component>();
		}

		public virtual void Update(double dt)
		{
		}

		public virtual void OnDeath()
		{

		}
		// XXX: Dependency inversion.
		public virtual void ChangeRoom(Starmaze.Game.Room oldRoom, Starmaze.Game.Room newRoom)
		{

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

		public void RegisterEvents(Starmaze.Game.World w)
		{
			Console.WriteLine("{0} registering events", this);
			foreach (var c in Components) {
				c.RegisterEvents(w);
			}
		}

		public void UnregisterEvents(Starmaze.Game.World w)
		{
			foreach (var c in Components) {
				c.UnregisterEvents(w);
			}
		}
	}
}

