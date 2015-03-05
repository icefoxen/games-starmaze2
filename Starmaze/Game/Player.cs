using System;
using Starmaze.Engine;

namespace Starmaze.Game
{
	public class Player : Actor
	{
		public Player()
		{
			//RenderClass = "TexTestRenderer";
			RenderClass = "SpriteTestRenderer";
			Body = new Body(this);
			Body.AddGeom(new BoxGeom(new BBox(-5, -5, 5, 5)));
			var model = Resources.TheResources.GetModel("TestModel");
			RenderParams = new StaticRendererParams(model);
			Components.Add(new InputController(this));

			var tex = Resources.TheResources.GetTexture("animtest");
			var atlas = new TextureAtlas(tex, 4, 4);
			var anim = new Animation(4, 0.51);
			var anim2 = new Animation(2, 0.2);
			var sprite = new Sprite(this, atlas, anim);
			sprite.AddAnimation(anim2);
			sprite.CurrentAnim = 1;
			Components.Add(sprite);
			KeepOnRoomChange = true;
		}
	}
}

