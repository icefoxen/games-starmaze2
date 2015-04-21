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
		// Generics remove the need to do all the casts.
		Dictionary<string, GUIText> guiHash;

		public GUI(double width, double height)
		{
			guiHash = new Dictionary<string, GUIText>();       
		}

		public void Draw(Vector2d cameraOffset)
		{
			//Need to figure out a proper way to keep GUI in place based
			//on the cameras offset. Calculate the difference of the camera offset 
			//each frame and move the GUI in the opposite direction 
            
            
			//((GUIRenderState)guiActor.RenderState).Texture = texture;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="pos"></param>
		/// <param name="fontsize"></param>
		public void DrawString(string text, Vector2d pos, int fontsize = 24)
		{
           
		}

		public void LoadString(string name)
		{
			texture = Resources.TheResources.GetStringTexture(name);
		}

		/// <summary>
		/// Creates a GUI Text object.
		/// </summary>
		/// <param name="world"></param>
		/// <param name="pos"></param>
		/// <param name="text"></param>
		/// <param name="fontsize"></param>
		/// <returns>The key for the GUIText created. The key can be used as the editGUIText parameter key to edit the GUIActor</returns>
		public string CreateGUIText(World world, Vector2d pos, string text = "", int fontsize = 24)
		{
			GUIText gActor = new GUIText(world, pos, text, fontsize);

			// Using + to concatenate strings causes more allocations,
			string key = String.Format("{0}{1}", pos.X, pos.Y);
			guiHash.Add(key, gActor);

			return key;
		}

		/// <summary>
		/// Allows us to edit any of GUI Text simply by giving the appropriate key.
		/// 
		/// </summary>
		/// <param name="key">The key value</param>
		/// <param name="text">The text to draw on the screen</param>
		/// <param name="fontsize"></param>
		public void editGUIText(string key, string text, int fontsize = 24)
		{
			if (guiHash.ContainsKey(key)) {
				guiHash[key].DrawString(text, fontsize);
			}
		}
	}
	//An object that
	public class GUIText
	{
		Actor actor;
		Texture texture;

		public GUIText(World world, Vector2d pos, string text = "", int fontsize = 24)
		{
			//Set up Actor
			actor = new Actor();
			//actor.Body = new Body(actor, false, true);
			//Set the actors position
			//actor.Body.MoveTo(pos);

			//Set up the GUIRenderState for the Actor
			texture = TextDrawer.RenderString(text, Color4.White, fontsize);
			GUIRenderState guiRState = new GUIRenderState(actor, texture);
			guiRState.Scale = new Vector2(2.5f, 1);
			actor.RenderState = guiRState;

			world.AddActor(actor);
		}

		public void DrawString(string text, int fontsize = 24)
		{
			//For now this is a refernce for the positions of gui texts, may vary based on fontsize
			//left -65
			//right 90
			//top 70
			//bottom -80
			((GUIRenderState)actor.RenderState).Texture = TextDrawer.RenderString(text, Color4.White, fontsize);
		}
	}
}
/*Have a method for displaying multiple GUI rendered text
 * by making mutliple GUI Actor objects
 * Now need a way to edit said objects after they have been initialized
         */