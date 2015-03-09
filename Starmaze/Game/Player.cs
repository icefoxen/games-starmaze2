using System;
using Starmaze.Engine;
using OpenTK;

namespace Starmaze.Game
{
	public class Player : Actor
	{
		public Player()
		{
			Body = new Body(this);
			Body.AddGeom(new BoxGeom(new BBox(-5, -5, 5, 5)));
			Components.Add(new InputController(this));

			var tex = Resources.TheResources.GetTexture("animtest");
			var atlas = new TextureAtlas(tex, 4, 4);
			var anim = new Animation(4, 0.51);
			var anim2 = new Animation(2, 0.2);
			var sprite = new Sprite(this, atlas, anim);
			sprite.AddAnimation(anim2);
			sprite.CurrentAnim = 0;
			Components.Add(sprite);
			RenderState = new SpriteRenderState(this, sprite);

			var t = TextDrawer.RenderString("The quick white fox jumps over the lazy dog", OpenTK.Graphics.Color4.White, fontSize: 36);
			RenderState = new BillboardRenderState(this, t, scale: new Vector2(15, 1));
			KeepOnRoomChange = true;
		}
	}
}

