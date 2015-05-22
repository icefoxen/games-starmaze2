using System;
using Newtonsoft.Json;
using OpenTK;
using Dyn = FarseerPhysics.Dynamics;

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
		OnCollision = 1 << 4,
		OnSeparation = 1 << 5,
	}
	// Other event types: OnRoomChange?  OnZoneChange?  Plus physics-based ones, of course...
	/// <summary>
	/// Base class for all Components.  Actors are made of Components.
	/// </summary>
	public class Component
	{
		public Actor Owner;
		public EventType HandledEvents;
        public RenderState RenderState=null;

		public Component()
		{
			HandledEvents = EventType.None | EventType.OnCollision | EventType.OnSeparation;
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
			if (HandledEvents.HasFlag(EventType.OnCollision)) {
				Log.Assert(Owner != null);
				Log.Assert(Owner.Body != null);
				Owner.Body.Fixture.OnCollision += OnCollision;
			}
			if (HandledEvents.HasFlag(EventType.OnSeparation)) {
				Log.Assert(Owner != null);
				Log.Assert(Owner.Body != null);
				Owner.Body.Fixture.OnSeparation += OnSeparation;
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
			if (HandledEvents.HasFlag(EventType.OnCollision)) {
				Log.Assert(Owner != null);
				Log.Assert(Owner.Body != null);
				Owner.Body.Fixture.OnCollision -= OnCollision;
			}
			if (HandledEvents.HasFlag(EventType.OnSeparation)) {
				Log.Assert(Owner != null);
				Log.Assert(Owner.Body != null);
				Owner.Body.Fixture.OnSeparation -= OnSeparation;
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

		public virtual bool OnCollision(Dyn.Fixture f1, Dyn.Fixture f2, Dyn.Contacts.Contact contact)
		{
			Log.Message("Collision happened: {0}, {1}, {2}", f1.Body.UserData, f2.Body.UserData, contact);
			return true;
		}

		public virtual void OnSeparation(Dyn.Fixture f1, Dyn.Fixture f2)
		{
			Log.Message("Separation happend");
		}

		public virtual void PreSave()
		{

		}

		public virtual void PostLoad()
		{

		}
	}
}

