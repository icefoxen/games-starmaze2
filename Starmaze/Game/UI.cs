using System;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using Starmaze.Engine;
using System.Drawing;
using System.Collections.Generic;

namespace Starmaze.Game
{
	public class GUI
	{
		Texture texture;
		public Actor guiActor;
        List<GUIActor> guiActorList;

		public GUI(double width, double height)
		{
            guiActorList = new List<GUIActor>();

			Bitmap bmp = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			guiActor = new Actor();
			guiActor.Body = new Body(guiActor,false,true);
            guiActor.Body.MoveTo(new Vector2d(0,0));
            texture = new Texture(bmp);
			GUIRenderState gRenderState = new GUIRenderState(guiActor, texture);

            gRenderState.Scale = new Vector2(2.5f,1);
            guiActor.RenderState = gRenderState;          
        }

		public void Draw(Vector2d cameraOffset)
		{
            //Need to figure out a proper way to keep GUI in place based
            //on the cameras offset. Calculate the difference of the camera offset 
            //each frame and move the GUI in the opposite direction 
            ((GUIRenderState)guiActor.RenderState).Texture = texture;
		}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pos"></param>
        /// <param name="fontsize"></param>
        public void DrawString(string text, Vector2d pos,int fontsize =24)
        {
            //For now this is my refernce for the positions
            //left -65
            //right 90
            //top 70
            //bottom -80
            texture = TextDrawer.RenderString(text, Color4.White, fontsize);
            guiActor.Body.MoveTo(pos);
        }

		public void LoadString(string name)
		{
			texture = Resources.TheResources.GetStringTexture(name);
		}

        public GUIActor CreateGUIActor(World world, Vector2d pos, string text="", int fontsize = 24)
        {
            GUIActor gActor = new GUIActor(world,pos,text,fontsize);

            guiActorList.Add(gActor);

            return gActor;
        }

	}

    public class GUIActor
    {
        Actor actor;
        Texture texture;

        public GUIActor(World world, Vector2d pos, string text = "", int fontsize = 24)
        {
            //Set up Actor
            actor = new Actor();
            actor.Body = new Body(actor, false, true);
            //Set the actors position
            actor.Body.MoveTo(pos);

            //Set up the GUIRenderState for the Actor
            texture = TextDrawer.RenderString(text, Color4.White, fontsize);
            GUIRenderState guiRState = new GUIRenderState(actor, texture);
            guiRState.Scale = new Vector2(2.5f, 1);
            actor.RenderState = guiRState;

            world.AddActor(actor);
        }

        public void DrawString(string text, int fontsize = 24)
        {
            //textToDraw += text;
            texture = TextDrawer.RenderString(text, Color4.White, fontsize);
        }
    }  
}
/*Have a method for displaying multiple GUI rendered text
 * by making mutliple GUI Actor objects
 * Now need a way to edit said objects after they have been initialized
         */