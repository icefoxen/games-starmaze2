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
<<<<<<< working copy
	public class TextDrawer
	{
		public static Texture RenderString(string s, Color4 color, int fontSize = 24)
		{
			var textColor = Util.FromColor4(color);
			var font = new Font(FontFamily.GenericMonospace, fontSize, FontStyle.Bold);
			var dummy = new Bitmap(1, 1);
			SizeF size;
			using (var graphics = System.Drawing.Graphics.FromImage(dummy)) {
				// We need this stupid dummy drawing context because System.Drawing.Graphics is sorta lame and assumes 
				// you never need to know how big a string is _before_ allocating memory for it.
				// The alternative is adding a dependency on Windows.Forms, so, forget that.
				size = graphics.MeasureString(s, font);
			}
			// BUGGO: This squishes the drawn text into a square shape no matter what shape it is!
			var pow2Width = (int)SMath.RoundUpToPowerOf2(size.Width);
			var pow2Height = (int)SMath.RoundUpToPowerOf2(size.Height);
			var bitmap = new Bitmap(pow2Width, pow2Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			using (var graphics = System.Drawing.Graphics.FromImage(bitmap)) {
				graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, pow2Width, pow2Height);
				graphics.DrawString(s, font, new SolidBrush(textColor), 0, 0);
				graphics.Flush();
			}
			return new Texture(bitmap);
		}
	}
=======
    public class GUI
    {
        Texture texture;
        Bitmap bmp;
        float fontsize = 20;
        public Actor guiActor;
        string textToDraw;
>>>>>>> destination

<<<<<<< working copy
	public class GUI
	{
		Bitmap bmp;
		Texture text_texture;
		TextRenderer text_renderer;
		float fontsize = 20;
		Font serif, sans, mono;
		int bit_dim;
		protected GLDiscipline discipline;
		protected Shader shader;

		#region Constructor

		public GUI(int dimension)
		{
			bit_dim = (int)Math.Pow(2, nearestPow(dimension));
			text_renderer = new TextRenderer();
			bmp = new Bitmap(bit_dim, bit_dim, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			text_renderer.zOrder = ZOrder.GUI;
			serif = new Font(FontFamily.GenericSerif, fontsize);
			sans = new Font(FontFamily.GenericSansSerif, fontsize);
			mono = new Font(FontFamily.GenericMonospace, fontsize);
			/*renderer.Clear(Color.MidnightBlue);
            renderer.DrawString("The quick brown fox jumps over the lazy dog", serif, Brushes.White, position);
            position.Y += serif.Height;
            renderer.DrawString("The quick brown fox jumps over the lazy dog", sans, Brushes.White, position);
            position.Y += sans.Height;
            renderer.DrawString("The quick brown fox jumps over the lazy dog", mono, Brushes.White, position);
            position.Y += mono.Height;
            */
		}

		#endregion

		#region Draw

		/*The three ingredients to draw something are
              1. geometry to draw
              2. a shader to do the drawing
              3. and data to feed into the shader (including a texture)
             * So you say shader.Enable(); texture.Enable(); shader.loadRandomStuff(yourinputdata); geometry.Draw(); shader.Disable(); texture.Disable()
             *But Nigel, the 'geometry.Draw()' is the go button, everything else is basically setting the right parameters for that.
             */
		public void Draw(ViewManager view)
		{
			PointF position = PointF.Empty;
			// DrawString(view, "" + Physics.PHYSICS_HZ, serif, Brushes.White, position);
			DrawString(view, "The quick brown fox jumps over the lazy dog \njumps over the lazy dog", serif, Brushes.White, position);
			position.Y += 10;
			//DrawString(view, "The quick brown fox jumps over the lazy dog", sans, Brushes.White, position); position.Y += 10;
			//DrawString(view, "The quick brown fox jumps over the lazy dog", mono, Brushes.White, position);
			text_renderer.tex = new Texture(bmp);
			text_renderer.RenderText(view, new Vector2(position.X, position.Y), new Vector2(bit_dim / 64, bit_dim / 64));
		}
=======
       public GUI()
        {
            bmp = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            guiActor = new Actor();
            guiActor.Body = new Body(guiActor,false,true);
            BillboardRenderState billBRState = new BillboardRenderState(guiActor, new Texture(bmp));
            billBRState._scale = new Vector2(5,2);
            guiActor.RenderState = billBRState;
            textToDraw = "";         
           //DrawString("The quick brown\n fox jumps over the lazy dog \njumps over the lazy dog", new PointF());
           
        }
>>>>>>> destination

<<<<<<< working copy
		#endregion
=======
        public void Draw()
        {
           ((BillboardRenderState)guiActor.RenderState).Texture = texture;
        }
>>>>>>> destination

<<<<<<< working copy
		/// <summary>
		/// Draws the specified string to the backing store.
		/// </summary>
		/// <param name="text">The <see cref="System.String"/> to draw.</param>
		/// <param name="font">The <see cref="System.Drawing.Font"/> that will be used.</param>
		/// <param name="brush">The <see cref="System.Drawing.Brush"/> that will be used for the color of text.</param>
		/// <param name="point">The location of the text (at least it should be)
		/// The origin (0, 0) lies at the top-left corner of the backing store.</param>
		public void DrawString(ViewManager view, string text, Font font, Brush brush, PointF point)
		{
			text_renderer.RenderStart();
=======
        /// <summary>
        /// Draws the specified string to the backing store.
        /// </summary>
        /// <param name="text">The <see cref="System.String"/> to draw.</param>
        /// <param name="brush">The <see cref="System.Drawing.Brush"/> that will be used for the color of text.</param>
        /// <param name="point">The location of the text (at least it should be)
        /// The origin (0, 0) lies at the top-left corner of the backing store.</param>
        public void DrawString(string text, PointF point)
        {          
            textToDraw += text;
            texture = generateTextTexture();
        }

        public void LoadString(string name)
        {
            texture = Resources.TheResources.GetStringTexture(name);
        }
>>>>>>> destination

<<<<<<< working copy
			using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bmp)) {
				//Finds the size that the text would take up on the screen (width, height)
				SizeF size = graphics.MeasureString(text, font);

				graphics.FillRectangle(new SolidBrush(Color.Empty), 0, 0, bmp.Width, bmp.Height);

				graphics.DrawString(text, font, brush, point.X, point.Y);

				graphics.Flush();
				graphics.Dispose();
			}

		}
=======
        public Texture generateTextTexture()
        {
            Vector2 size = getTextSize();
            bmp = new Bitmap((int)size.X, (int)size.Y);
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bmp))
            {
                graphics.FillRectangle(new SolidBrush(Color.Empty), 0, 0, bmp.Width, bmp.Height);
                graphics.DrawString(textToDraw, new Font(FontFamily.GenericMonospace, fontsize), Brushes.White, new PointF());
                graphics.Flush();
                graphics.Dispose();
            }
            Resources.TheResources.addStringTexture("test", bmp);
            textToDraw = "";
            return new Texture(bmp);
        }
>>>>>>> destination

<<<<<<< working copy
=======
        public Vector2 getTextSize()
        {
            System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bmp);
            //Finds the size that the text would take up on the screen (width, height)
            SizeF size = graphics.MeasureString(textToDraw, new Font(FontFamily.GenericSerif, fontsize));
            Vector2 vector2 = new Vector2((float)SMath.RoundUpToPowerOf2(size.Width), (float)SMath.RoundUpToPowerOf2(size.Height));
            return vector2;
        }
    }
>>>>>>> destination

<<<<<<< working copy
		int nearestPow(int size)
		{
			int count = 0;
			for (int i = 0; i < size; i++) {
				if (i % Math.Pow(2, count) == 0)
					count++;
			}

			return count - 1;

		}

	}
=======
    public class TextMap
    {
        string text;
        PointF position;
        
        public TextMap(string _text, PointF _pos)
        {
            text = _text;
            position = _pos;
        }
    }
>>>>>>> destination
}