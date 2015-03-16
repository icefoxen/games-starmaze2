using System;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using Starmaze.Engine;
using System.Drawing;

namespace Starmaze.Game
{
	public class GUI
	{
		Texture texture;
		Bitmap bmp;
		float fontsize = 20;
		public Actor guiActor;
		string textToDraw;

		public GUI()
		{
			bmp = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			guiActor = new Actor();
			guiActor.Body = new Body(guiActor, true,false);
			BillboardRenderState billBRState = new BillboardRenderState(guiActor, new Texture(bmp));
			billBRState.Scale = new Vector2(5, 2);
			guiActor.RenderState = billBRState;
			textToDraw = "";
            texture = new Texture(bmp);
			//DrawString("The quick brown\n fox jumps over the lazy dog \njumps over the lazy dog", new PointF());
		}

		public void Draw()
		{
            ((BillboardRenderState)guiActor.RenderState).Texture = texture;
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
			texture = TextDrawer.RenderString(text, Color4.White);
		}


		public void LoadString(string name)
		{
			texture = Resources.TheResources.GetStringTexture(name);
		}

		public Texture generateTextTexture()
		{
			Vector2 size = getTextSize();
			bmp = new Bitmap((int)size.X, (int)size.Y);
			using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bmp)) {
				graphics.FillRectangle(new SolidBrush(Color.Empty), 0, 0, bmp.Width, bmp.Height);
				graphics.DrawString(textToDraw, new Font(FontFamily.GenericMonospace, fontsize), Brushes.White, new PointF());
				graphics.Flush();
				graphics.Dispose();
			}
			textToDraw = "";
			return new Texture(bmp);
		}

		public Vector2 getTextSize()
		{
			System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bmp);
			//Finds the size that the text would take up on the screen (width, height)
			SizeF size = graphics.MeasureString(textToDraw, new Font(FontFamily.GenericSerif, fontsize));
			Vector2 vector2 = new Vector2((float)SMath.RoundUpToPowerOf2(size.Width), (float)SMath.RoundUpToPowerOf2(size.Height));
			return vector2;
		}
	}

}
