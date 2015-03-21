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

		public GUI(double width, double height)
		{
			Bitmap bmp = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			guiActor = new Actor();
			guiActor.Body = new Body(guiActor,false,true);
            guiActor.Body.MoveTo(new Vector2d(0,0));
            texture = new Texture(bmp);
			GUIRenderState gRenderState = new GUIRenderState(guiActor, texture);

            gRenderState.Scale = new Vector2(5, 2);
            guiActor.RenderState = gRenderState;            
            
			//DrawString("The quick brown\n fox jumps over the lazy dog \njumps over the lazy dog", new PointF());
		}

		public void Draw(Vector2d cameraOffset)
		{
            //Need to figure out a proper way to keep GUI in place based
            //on the cameras offset. Calculate the difference of the camera offset 
            //each frame and move the GUI in the opposite direction 
            ((GUIRenderState)guiActor.RenderState).Texture = texture;
           
		}

		/// <summary>
		/// Draws the specified string to the backing store.
		/// </summary>
		/// <param name="text">The <see cref="System.String"/> to draw.</param>
		/// <param name="brush">The <see cref="System.Drawing.Brush"/> that will be used for the color of text.</param>
		/// <param name="point">The location of the text (at least it should be)
		/// The origin (0, 0) lies at the top-left corner of the backing store.</param>
		public void DrawString(string text)
		{          
			//textToDraw += text;
			texture = TextDrawer.RenderString(text, Color4.White,12);
		}

		public void LoadString(string name)
		{
			texture = Resources.TheResources.GetStringTexture(name);
		}

	}

    public class GUIActors
    {
        //the body's position
        //the actors texture
        List<Actor> actors;

        public GUIActors()
        {
            
        }

        //Once the GUIRenderer is working properly will update this
        //and start using multiple gui actors to be able to show
        //multiple text messages on screen
        public void addNewGUI(Vector2d pos,Texture texture){
            Bitmap bmp = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Actor guiActor = new Actor();
            guiActor.Body = new Body(guiActor, false, true);
            //Set the actors position
            guiActor.Body.MoveTo(pos);
            BillboardRenderState billBRState = new BillboardRenderState(guiActor, texture);
            billBRState.Scale = new Vector2(5, 2);
            guiActor.RenderState = billBRState;            
        }
    }
}
