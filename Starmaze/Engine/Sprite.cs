using System;
using System.Collections.Generic;
using OpenTK;

namespace Starmaze.Engine
{
	public class TextureAtlas
	{
		readonly Texture Tex;
		readonly int Width;
		readonly int Height;

		public TextureAtlas(Texture tex, int width, int height)
		{
			Log.Assert(tex != null);
			Log.Assert(width > 0);
			Log.Assert(height > 0);
			Tex = tex;
			Width = width;
			Height = height;
		}

		public void Enable()
		{
			Tex.Enable();
		}

		public void Disable()
		{
			Tex.Disable();
		}

		public double ItemWidth()
		{
			return 1.0 / Width;
		}

		public double ItemHeight()
		{
			return 1.0 / Height;
		}

		public double OffsetX(int offset)
		{
			Log.Assert(offset < Width);
			return offset * ItemWidth();
		}

		public double OffsetY(int offset)
		{
			Log.Assert(offset < Height);
			return offset * ItemHeight();
		}

		public Vector2d Offset(int xoff, int yoff)
		{
			return new Vector2d(OffsetX(xoff), OffsetY(yoff));
		}
		// Construct a texture atlas from a series of bitmaps
		// possibly as a tool so we can construct atlases from segments.
		//public static TextureAtlas Construct(System.Drawing.Bitmap[] images)
		//{
		//	return null;
		//}
	}

	/// <summary>
	/// A sequence of delays that keeps track of a state; it is utterly independent of 
	/// what it is actually animating.
	/// </summary>
	public class Animation
	{
		public int MaxFrame {
			get {
				return Delays.Length;
			}
		}

		public int Frame;
		public double[] Delays;
		double LastUpdate;

		public Animation(double[] frames)
		{
			Frame = 0;
			LastUpdate = 0;
			Delays = frames;
		}

		public Animation(int maxframes, double delay) : this(Util.FillArray(maxframes, delay))
		{
		}

		public void Update(double dt)
		{
			LastUpdate += dt;
			if (LastUpdate >= Delays[Frame]) {
				LastUpdate -= Delays[Frame];
				Frame = (Frame + 1) % MaxFrame;
			}
		}
	}

	/// <summary>
	/// An animated, textured billboard.
	/// </summary>
	public class Sprite : Component
	{
		public TextureAtlas Atlas;
		List<Animation> Animations;
		public int CurrentAnim;

		public Sprite(Actor owner, TextureAtlas atlas, IEnumerable<Animation> anim) : base(owner)
		{
			Log.Assert(atlas != null);
			Atlas = atlas;
			Animations = new List<Animation>(anim);
			Log.Assert(Animations.Count > 0);
			CurrentAnim = 0;
			HandledEvents = EventType.OnUpdate;
		}

		public Sprite(Actor owner, TextureAtlas atlas, Animation anim) : this(owner, atlas, new Animation[] { anim })
		{
		}

		public void AddAnimation(Animation anim)
		{
			Animations.Add(anim);
		}

		/// <summary>
		/// Returns UV coordinates for this sprite's current animation frame.
		/// </summary>
		/// <returns>Box2(x0, y0, w, h)</returns>
		public Vector4 GetBox()
		{
			var anim = Animations[CurrentAnim];
			var frame = anim.Frame;
			var x = Atlas.OffsetX(frame);
			var y = Atlas.OffsetY(CurrentAnim);
			var w = Atlas.ItemWidth();
			var h = Atlas.ItemHeight();
			return new Vector4((float)x, (float)y, (float)w, (float)h);
		}

		public override void OnUpdate(object sender, FrameEventArgs args)
		{
			var dt = args.Time;
			Animations[CurrentAnim].Update(dt);
		}
	}
}

