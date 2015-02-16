using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{

	/// <summary>
	/// A class that represents a single step in a graphics post-processing pipeline.
	/// It takes a texture and renders it to a new texture with a particular shader.
	/// </summary>
	// TODO: Make it so each step can accept multiple inputs and produce multiple outputs.
	// But how to wire them together to feed one into the other requires some thought.
	public class PostprocStep : IDisposable
	{
		int width;
		int height;
		public Texture DestTexture;
		FramebufferObject fbo;
		Shader shader;
		VertexArray bb;
		Texture tex;
		Matrix4 matrix;

		public PostprocStep(Shader shader, int screenw, int screenh)
		{
			Log.Assert(Util.IsPowerOf2(screenw));
			Log.Assert(Util.IsPowerOf2(screenh));
			width = screenw;
			height = screenh;
			this.shader = shader;
			// XXX: This could be more efficient somehow...
			matrix = Matrix4.CreateOrthographic(1, 1, 0, 10f);

			tex = Resources.TheResources.GetTexture("playertest");

			// Create back-buffer
			// XXX: Is ActiveTexture needed?  I THINK so...
			//GL.ActiveTexture(TextureUnit.Texture0);
			DestTexture = new Texture(width, height);

			// Create framebuffer
			fbo = new FramebufferObject(DestTexture);
			//this.bb = Starmaze.Content.Images.Billboard();

			// Make a billboard to render to.
			// XXX: Aspect ratio...
			var aspectRatio = 4.0f / 3.0f;
			var bb = new VertexList(VertexLayout.TextureVertex);

			var halfHeight = 1.0f / 2;
			var halfWidth = 1.0f / 2;

			bb.AddTextureVertex(
				new Vector2(-halfWidth, -halfHeight),
				Color4.White,
				new Vector2(0, 0)
			);
			bb.AddTextureVertex(
				new Vector2(-halfWidth, halfHeight),
				Color4.White,
				new Vector2(0, (1.0f / aspectRatio))
			);
			bb.AddTextureVertex(
				new Vector2(halfWidth, halfHeight),
				Color4.White,
				new Vector2(1, (1.0f / aspectRatio))
			);
			bb.AddTextureVertex(
				new Vector2(halfWidth, -halfHeight),
				Color4.White,
				new Vector2(1, 0)
			);
			var indices = new uint[] {
				0, 1, 2,
				0, 2, 3,
			};
			this.bb = new VertexArray(shader, bb, idxs: indices);

		}

		/// <summary>
		/// Rescale frame buffer object.  If not given a power of 2 size,
		/// scales it up until it is.
		/// </summary>
		/// <param name="width">Screen width.</param>
		/// <param name="height">Screen height.</param>
		public void Resize(int width, int height)
		{
			Log.Assert(Util.IsPowerOf2(width));
			Log.Assert(Util.IsPowerOf2(height));
			DestTexture.ClearAndResize(width, height);
		}

		public void Render(Texture fromTexture, bool final = false)
		{
			if (!final) {
				fbo.Enable();
			}
			Graphics.TheGLTracking.SetShader(shader);
			fromTexture.Enable();
			//shader.Uniformi("texture", fromTexture.Handle);
			//tex.Enable();
			shader.UniformMatrix("projection", matrix);
			shader.Uniformi("texture", 0);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			bb.Draw();
			Graphics.TheGLTracking.SetShader(null);
			if (!final) {
				fbo.Disable();
			}
		}


		private bool disposed = false;

		~PostprocStep()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			// Don't run the finalizer, since it's a waste of time.
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed) {
				return;
			}
			disposed = true;
			if (disposing) {
				// Clean up managed resources
				// These are managed resources that
				// aren't exactly scarce or high-turnover; we can probably just let the GC handle them.
				//fbo.Dispose();
				//DestTexture.Dispose();
			}
			// Clean up unmanaged resources
		}
	}

	/// <summary>
	/// A class that represents a post-processing pipeline,
	/// which is a set of shader passes that occur after the main drawing step.
	/// </summary>
	public class PostprocPipeline
	{
		int width;
		int height;
		List<PostprocStep> steps;
		FramebufferObject fbo;
		Texture fboTexture;

		public PostprocPipeline(int width, int height)
		{
			this.width = (int)SMath.RoundUpToPowerOf2(width);
			this.height = (int)SMath.RoundUpToPowerOf2(height);
			// Sanity checking.
			Log.Assert(Util.IsPowerOf2(this.width));
			Log.Assert(Util.IsPowerOf2(this.height));
			steps = new List<PostprocStep>();
			fboTexture = new Texture(this.width, this.height);
			fbo = new FramebufferObject(fboTexture);
		}

		public void AddStep(Shader shader)
		{
			var step = new PostprocStep(shader, width, height);
			steps.Add(step);
		}

		/// <summary>
		/// Takes a function that draws the scene, and draws everything with the postprocessing pipeline.
		/// </summary>
		/// <param name="thunk">Thunk.</param>
		public void RenderWith(Action drawScene)
		{
			// Render initial scene to FBO
			fbo.Enable();
			GL.ClearColor(Color4.Black);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			drawScene();
			fbo.Disable();

			var fromTexture = fboTexture;

			// Then we take the drawn scene and run it through each of the postprocessing steps.
			for (int i = 0; i < steps.Count - 1; i++) {
				var step = steps[i];
				step.Render(fromTexture, final: false);
				fromTexture = step.DestTexture;
			}
			// Then do the final rendering to the screen.
			steps[steps.Count - 1].Render(fromTexture, final: true);
		}

		public void Resize(int width, int height)
		{
			var width2 = (int)SMath.RoundUpToPowerOf2(width);
			var height2 = (int)SMath.RoundUpToPowerOf2(height);
			fboTexture.ClearAndResize(width2, height2);
			foreach (var ppstep in steps) {
				ppstep.Resize(width2, height2);
			}
		}
	}
}

