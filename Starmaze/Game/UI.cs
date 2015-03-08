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

        Bitmap bmp;
        float fontsize = 20;
        public Actor guiActor;
        string textToDraw;


        struct TextMap
        {
            string text;
            PointF position;
        }

       public GUI()
        {
            bmp = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            guiActor = new Actor();
            guiActor.Body = new Body(guiActor,false,true);
            BillboardRenderState billBRState = new BillboardRenderState(guiActor, new Texture(bmp));
            billBRState._scale = new Vector2(6,3);
            guiActor.RenderState = billBRState;
            textToDraw = "";
        }

        public void Draw(ViewManager view)
        {
            DrawString("The quick brown\n fox jumps over the lazy dog \njumps over the lazy dog",new PointF());
            //((BillboardRenderState)guiActor.RenderState).Texture = new Texture(generateBitmap());
        }

        /// <summary>
        /// Draws the specified string to the backing store.
        /// </summary>
        /// <param name="text">The <see cref="System.String"/> to draw.</param>
        /// <param name="brush">The <see cref="System.Drawing.Brush"/> that will be used for the color of text.</param>
        /// <param name="point">The location of the text (at least it should be)
        /// The origin (0, 0) lies at the top-left corner of the backing store.</param>
        public void DrawString(string text, PointF point)
        {
           /* Bitmap newbmp;
            System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bmp);
            //Finds the size that the text would take up on the screen (width, height)
            SizeF size = graphics.MeasureString(text,new Font(FontFamily.GenericSerif, fontsize));
            int w=Convert.ToInt32(SMath.RoundUpToPowerOf2(size.Width));
            int h=Convert.ToInt32(SMath.RoundUpToPowerOf2(size.Height));
            
            newbmp = new Bitmap(w,h,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            graphics = System.Drawing.Graphics.FromImage(newbmp);
            graphics.FillRectangle(new SolidBrush(Color.Empty), 0, 0, newbmp.Width, newbmp.Height);
            graphics.DrawString(text, new Font(FontFamily.GenericSerif, fontsize), Brushes.White, point.X, point.Y);
            graphics.Flush();
            graphics.Dispose();
            
            bmp = newbmp;*/
            textToDraw = text;
            ((BillboardRenderState)guiActor.RenderState).Texture = new Texture(generateBitmap());
        }

        public Bitmap generateBitmap()
        {
            Bitmap newbmp;
            Vector2 size = getTextSize();

            newbmp = new Bitmap((int)size.X, (int)size.Y);
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(newbmp))
            {
                graphics.FillRectangle(new SolidBrush(Color.Empty), 0, 0, newbmp.Width, newbmp.Height);
                graphics.DrawString(textToDraw, new Font(FontFamily.GenericSerif, fontsize), Brushes.White, new PointF());
                graphics.Flush();
                graphics.Dispose();
            }
            return newbmp;

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