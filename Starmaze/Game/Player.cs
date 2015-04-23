using System;
using Starmaze.Engine;
using OpenTK;

namespace Starmaze.Game
{
	public class Player : Actor
	{
		public Player()
		{
			AddComponent(new FBody());
			//Body.AddGeom(new BoxGeom(new BBox(-5, -15, 5, 5)));
			AddComponent(new InputController());
			AddComponent(new Life(10));

			var tex = Resources.TheResources.GetTexture("PlayerAssetAnimationTestSpriteSheetv3");
			var atlas = new TextureAtlas(tex, 16, 1);
			var anim = new Animation(10, 0.2);
			var anim2 = new Animation(2, 0.2);
			AddComponent(new SpriteRenderState(atlas, new Animation[] { anim, anim2 }, scale: new Vector2(3f, 3f)));

			//var t = TextDrawer.RenderString("The quick white fox jumps over the zephyr-blessed dragon", OpenTK.Graphics.Color4.White, fontSize: 36);
			//RenderState = new BillboardRenderState(this, t, scale: new Vector2(15, 1));
			KeepOnRoomChange = true;
		}
	}
}

