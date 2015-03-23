using System;
using Newtonsoft.Json;
using OpenTK;

namespace Starmaze.Engine
{
	[Flags]
	public enum EventType
	{
		None = 0,
		OnUpdate = 1 << 0,
		OnKeyDown = 1 << 1,
		OnKeyUp = 1 << 2,
		OnDeath = 1 << 3,
	}
	// Other event types: OnRoomChange?  OnZoneChange?  Plus physics-based ones, of course...
	/// <summary>
	/// Base class for all Components.  Actors are made of Components.
	/// </summary>
	public class Component : Starmaze.Game.ISaveLoadable
	{
		public Actor Owner;
		public EventType HandledEvents;

		public Component(Actor owner)
		{
			Owner = owner;
			HandledEvents = EventType.None;
		}
		// It would be nice if these were unnecessary; we could just provide a list of events somehow
		// that each component cares about and the system would be smart enough to add/remove them itself.
		public void RegisterEvents(World w)
		{
			//Console.WriteLine("{0} registering events", this);
			if (HandledEvents.HasFlag(EventType.OnUpdate)) {
				w.OnUpdate += OnUpdate;
			}
			if (HandledEvents.HasFlag(EventType.OnKeyDown)) {
				w.OnKeyDown += OnKeyDown;
			}
			if (HandledEvents.HasFlag(EventType.OnKeyUp)) {
				w.OnKeyUp += OnKeyUp;
			}
			if (HandledEvents.HasFlag(EventType.OnDeath)) {
				w.OnDeath += OnDeath;
			}
		}

		public void UnregisterEvents(World w)
		{
			if (HandledEvents.HasFlag(EventType.OnUpdate)) {
				w.OnUpdate -= OnUpdate;
			}
			if (HandledEvents.HasFlag(EventType.OnKeyDown)) {
				w.OnKeyDown -= OnKeyDown;
			}
			if (HandledEvents.HasFlag(EventType.OnKeyUp)) {
				w.OnKeyUp -= OnKeyUp;
			}
			if (HandledEvents.HasFlag(EventType.OnDeath)) {
				w.OnDeath -= OnDeath;
			}
		}

		public virtual void OnUpdate(object sender, FrameEventArgs args)
		{

		}

		public virtual void OnKeyDown(object sender, InputAction a)
		{

		}

		public virtual void OnKeyUp(object sender, InputAction a)
		{

		}

		public virtual void OnDeath(object sender, EventArgs args)
		{

		}

		public virtual void PreSave()
		{

		}

		public virtual void PostLoad()
		{

		}
	}
}

