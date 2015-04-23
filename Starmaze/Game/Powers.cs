using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using Starmaze.Engine;

namespace Starmaze.Game
{
	public enum PowerIndex
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
		PowerIndex Ordering { get; }

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
		public PowerIndex Ordering { get { return PowerIndex.None; } }

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
		public SortedList<PowerIndex, IPower> Powers;

		public IPower CurrentPower;

		public PowerSet(Actor owner) : base(owner)
		{
			Powers = new SortedList<PowerIndex, IPower>();
			HandledEvents = EventType.OnUpdate | EventType.OnKeyDown | EventType.OnKeyUp;
			var p = new NullPower();
			AddPower(p);
			SetCurrentPower(p);
		}

		public override void OnKeyDown(object sender, InputAction a)
		{
			Log.Message("Power key down: {0}", a);
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
			Log.Message("Power key up: {0}", a);
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
			// XXX: Not updating powers that aren't current has potential
			// implications for weapon cooldown times.
			// One solution is to do it Iji style and reset all cooldowns
			// when you swap powers.  Check how Cave Story does it.
			CurrentPower.Update(args.Time);
		}

		public void AddPower(IPower p)
		{
			if (Powers.ContainsKey(PowerIndex.None)) {
				Powers.Remove(PowerIndex.None);
			}
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
			var powerCount = (int)PowerIndex.Count;
			do {
				currentOrder = (currentOrder + 1) % powerCount;
			} while(!Powers.ContainsKey((PowerIndex)currentOrder));
			SetCurrentPower(Powers[(PowerIndex)currentOrder]);
		}

		public void PreviousPower()
		{
			var currentOrder = (int)CurrentPower.Ordering;
			var powerCount = (int)PowerIndex.Count;
			do {
				// XXX: Double-check that modulo of a negative number works the way we want.
				currentOrder = (currentOrder - 1) % powerCount;
			} while(!Powers.ContainsKey((PowerIndex)currentOrder));
			SetCurrentPower(Powers[(PowerIndex)currentOrder]);
		}

		public override string ToString()
		{
			var powerNames = Powers.Values.Select(power => power.GetType().Name);
			var powerStr = String.Join(", ", powerNames);
			return string.Format("[PowerSet: Powers={0}]", powerStr);
		}

		// These methods are for saving and loading.
		public int[] PowerList {
			get {
				var keys = Powers.Keys;
				return keys.Cast<int>().ToArray();
			}
			set {
				var powers = value.Select(powerNum => PowerFromIndex((PowerIndex)powerNum));
				foreach (var p in powers) {
					AddPower(p);
				}
			}
		}

		public IPower PowerFromIndex(PowerIndex p)
		{
			switch (p) {
				case PowerIndex.Beginnings:
					// XXX: Dependency inversion
					return new Content.Beginnings.BeginningsPower();
				default:
					return new NullPower();
			}
		}
	}
}

