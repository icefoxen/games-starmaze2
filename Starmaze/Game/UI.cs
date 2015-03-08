using System;
using System.Collections.Generic;
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
            DrawString(view, "The quick brown fox jumps over the lazy dog \njumps over the lazy dog", serif, Brushes.White, position); position.Y += 10;
            //DrawString(view, "The quick brown fox jumps over the lazy dog", sans, Brushes.White, position); position.Y += 10;
            //DrawString(view, "The quick brown fox jumps over the lazy dog", mono, Brushes.White, position);
            text_renderer.tex = new Texture(bmp);
            text_renderer.RenderText(view, new Vector2(position.X, position.Y), new Vector2(bit_dim / 64, bit_dim / 64));
        }

        #endregion

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

            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bmp))
            {
                //Finds the size that the text would take up on the screen (width, height)
                SizeF size = graphics.MeasureString(text, font);

                graphics.FillRectangle(new SolidBrush(Color.Empty), 0, 0, bmp.Width, bmp.Height);

                graphics.DrawString(text, font, brush, point.X, point.Y);

                graphics.Flush();
                graphics.Dispose();
            }

        }

        int nearestPow(int size)
        {
            int count = 0;
            for (int i = 0; i < size; i++)
            {
                if (i % Math.Pow(2, count) == 0)
                    count++;
            }

            return count - 1;

        }

    }
}