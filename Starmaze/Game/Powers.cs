using System;
using System.Collections.Generic;
using OpenTK;
using Starmaze.Engine;

namespace Starmaze.Game
{
	public enum PowerOrdering : int
	{
		None,
		Beginnings,
		Fire,
		Water,
		Earth,
		Air,
		Life,
		Death,
		Endings,
		Count,
	}

	public interface IPower
	{
		PowerOrdering Ordering { get; }

		void Fire1KeyDown();

		void Fire1KeyUp();

		void Fire1KeyHeld();

		void Fire2KeyDown();

		void Fire2KeyUp();

		void Fire2KeyHeld();

		void JumpKeyDown();

		void JumpKeyUp();

		void JumpKeyHeld();

		void DefendKeyDown();

		void DefendKeyUp();

		void DefendKeyHeld();

		void Update(double dt);
	}

	/// <summary>
	/// A Power that does nothing.
	/// </summary>
	public class NullPower : IPower
	{
		public PowerOrdering Ordering { get { return PowerOrdering.None; } }

		public void Fire1KeyDown()
		{
		}

		public void Fire1KeyUp()
		{
		}

		public void Fire1KeyHeld()
		{
		}

		public void Fire2KeyDown()
		{
		}

		public void Fire2KeyUp()
		{
		}

		public void Fire2KeyHeld()
		{
		}

		public void JumpKeyDown()
		{
			
		}

		public void JumpKeyUp()
		{
		}

		public void JumpKeyHeld()
		{
		}

		public void DefendKeyDown()
		{
		}

		public void DefendKeyUp()
		{
		}

		public void DefendKeyHeld()
		{
		}

		public void Update(double dt)
		{
		}
	}

	/// <summary>
	/// The set of the player's powers.
	/// All powers have four parts: Two attacks, a jump and a defense.
	/// This provides some indirection allowing powers to be swapped in and out while presenting
	/// a consistent interface to the player,
	/// as well as managing adding and switching powers.
	/// </summary>
	public class PowerSet : Component
	{
		SortedList<PowerOrdering, IPower> Powers;
		IPower CurrentPower;

		public PowerSet(Actor owner) : base(owner)
		{
			Powers = new SortedList<PowerOrdering, IPower>();
			HandledEvents = EventType.OnKeyDown | EventType.OnKeyUp | EventType.OnUpdate;
			var p = new NullPower();
			AddPower(p);
			SetCurrentPower(p);
		}

		public override void OnKeyDown(object sender, InputAction a)
		{
			switch (a) {
				case InputAction.Fire1:
					CurrentPower.Fire1KeyDown();
					break;
				case InputAction.Fire2:
					CurrentPower.Fire2KeyDown();
					break;
				case InputAction.Defend:
					CurrentPower.DefendKeyDown();
					break;
				case InputAction.Jump:
					CurrentPower.JumpKeyDown();
					break;
			}
		}

		public override void OnKeyUp(object sender, InputAction a)
		{
			switch (a) {
				case InputAction.Fire1:
					CurrentPower.Fire1KeyUp();
					break;
				case InputAction.Fire2:
					CurrentPower.Fire2KeyUp();
					break;
				case InputAction.Defend:
					CurrentPower.DefendKeyUp();
					break;
				case InputAction.Jump:
					CurrentPower.JumpKeyUp();
					break;
			}
		}

		public override void OnUpdate(object sender, FrameEventArgs args)
		{
			CurrentPower.Update(args.Time);
		}

		public void AddPower(IPower p)
		{
			Powers.Add(p.Ordering, p);
		}

		public void SetCurrentPower(IPower p)
		{
			CurrentPower = p;
		}

		public void NextPower()
		{
			// This is a little bit nasty but should work.
			var currentOrder = (int)CurrentPower.Ordering;
			var powerCount = (int)PowerOrdering.Count;
			do {
				currentOrder = (currentOrder + 1) % powerCount;
			} while(!Powers.ContainsKey((PowerOrdering)currentOrder));
			SetCurrentPower(Powers[(PowerOrdering)currentOrder]);
		}

		public void PreviousPower()
		{
			var currentOrder = (int)CurrentPower.Ordering;
			var powerCount = (int)PowerOrdering.Count;
			do {
				currentOrder = (currentOrder - 1) % powerCount;
			} while(!Powers.ContainsKey((PowerOrdering)currentOrder));
			SetCurrentPower(Powers[(PowerOrdering)currentOrder]);
		}
	}
}

