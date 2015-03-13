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
			AddComponent(new InputController(this));
			AddComponent(new Life(this, 10));

			var tex = Resources.TheResources.GetTexture("PlayerAssetAnimationTestSpriteSheet");
			var atlas = new TextureAtlas(tex, 8, 4);
			var anim = new Animation(5, 0.2);
			var anim2 = new Animation(2, 0.2);
			var sprite = new Sprite(this, atlas, anim);
			sprite.AddAnimation(anim2);
			sprite.CurrentAnim = 0;
			Components.Add(sprite);
			RenderState = new SpriteRenderState(this, sprite, scale: new Vector2(2f, 2f));

			var t = TextDrawer.RenderString("The quick white fox jumps over the zephyr-blessed dragon", OpenTK.Graphics.Color4.White, fontSize: 36);
			//RenderState = new BillboardRenderState(this, t, scale: new Vector2(15, 1));
			KeepOnRoomChange = true;
		}
	}
}

