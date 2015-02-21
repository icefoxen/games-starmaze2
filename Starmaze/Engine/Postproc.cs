using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	/*
	public interface IPostprocSource
	{
		Texture Source { get; }

		void Render();

		void AttachSink(IPostprocSink sink);
	}

	public interface IPostprocSink
	{
		Texture Sink { set; }

		void Render();

		void AttachSource(IPostprocSource source);
	}

	public interface IPostprocFilter : IPostprocSource, IPostprocSink
	{

		int bufferWidth { get; }

		int bufferHeight { get; }

		void Resize(int width, int height);

	}
*/


	public interface IPostprocStep
	{
		void Resize(int width, int height);

		Texture Render(Texture fromTexture, bool final = false);
	}

	public class GlowFilter : IPostprocStep
	{
		int width;
		int height;
		Texture DestTexture1;
		Texture DestTexture2;
		FramebufferObject fbo1;
		FramebufferObject fbo2;
		Shader hGlowShader;
		Shader vGlowShader;
		Shader combineShader;
		VertexArray bb1;
		VertexArray bb2;
		VertexArray bb3;
		Matrix4 matrix;

		public GlowFilter(int screenw, int screenh)
		{
			Log.Assert(Util.IsPowerOf2(screenw));
			Log.Assert(Util.IsPowerOf2(screenh));
			width = screenw;
			height = screenh;
			hGlowShader = Resources.TheResources.GetShader("glow1");
			vGlowShader = Resources.TheResources.GetShader("glow2");
			combineShader = Resources.TheResources.GetShader("glow3");
			// XXX: This could be more efficient somehow...
			matrix = Matrix4.CreateOrthographic(1, 1, 0, 10f);

			// Create back-buffer
			DestTexture1 = new Texture(width, height);
			DestTexture2 = new Texture(width, height);

			// Create framebuffer
			fbo1 = new FramebufferObject(DestTexture1);
			fbo2 = new FramebufferObject(DestTexture2);
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
			bb1 = new VertexArray(hGlowShader, bb, idxs: indices);
			bb2 = new VertexArray(vGlowShader, bb, idxs: indices);
			bb3 = new VertexArray(combineShader, bb, idxs: indices);

		}

		/// <summary>
		/// Rescale frame buffer object.  If not given a power of 2 size,
		/// scales it up until it is.
		/// </summary>
		/// <param name="width">Screen width.</param>
		/// <param name="height">Screen height.</param>
		public void Resize(int width, int height)
		{
			width = (int)SMath.RoundUpToPowerOf2(width);
			height = (int)SMath.RoundUpToPowerOf2(height);
			DestTexture1.ClearAndResize(width, height);
		}

		public Texture Render(Texture fromTexture, bool final = false)
		{
			const int convolutionRadius = 5;
			int texels = 512;
			// Do the first step of the separable convolution
			fbo1.Enable();
			fromTexture.Enable();
			Graphics.TheGLTracking.SetShader(hGlowShader);
			hGlowShader.UniformMatrix("projection", matrix);
			hGlowShader.Uniformi("texels", texels);
			hGlowShader.Uniformi("convolutionRadius", convolutionRadius);
			hGlowShader.Uniformi("texture", 0);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			bb1.Draw();
			fbo1.Disable();

			// Do the second step of the separable convolution
			fbo2.Enable();
			Graphics.TheGLTracking.SetShader(vGlowShader);
			DestTexture1.Enable();
			vGlowShader.UniformMatrix("projection", matrix);
			vGlowShader.Uniformi("texels", texels);
			vGlowShader.Uniformi("convolutionRadius", convolutionRadius);
			vGlowShader.Uniformi("texture", 0);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			bb2.Draw();
			fbo2.Disable();

			// Combine the blurred texture with the original texture again
			if (!final) {
				fbo1.Enable();
			}


			// Shady-ass multi-texturing
			var textures = new Texture[] {
				fromTexture,
				DestTexture2
			};
			Texture.EnableMultiple(textures);

			Graphics.TheGLTracking.SetShader(combineShader);
			combineShader.UniformMatrix("projection", matrix);
			combineShader.Uniformi("texture", 0);
			combineShader.Uniformi("glowTexture", 1);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			bb3.Draw();
			//DestTexture2.Enable();
			//bb2.Draw();
			Texture.DisableMultiple(textures);

			if (!final) {
				fbo1.Disable();
			}

			return DestTexture1;
		}
	}

	/// <summary>
	/// A class that represents a single step in a graphics post-processing pipeline.
	/// It takes a texture and renders it to a new texture with a particular shader.
	/// </summary>
	// TODO: Make it so each step can accept multiple inputs and produce multiple outputs.
	// But how to wire them together to feed one into the other requires some thought.
	public class PostprocStep : IPostprocStep
	{
		int width;
		int height;
		Texture DestTexture;
		FramebufferObject fbo;
		Shader shader;
		VertexArray bb;
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
			width = (int)SMath.RoundUpToPowerOf2(width);
			height = (int)SMath.RoundUpToPowerOf2(height);
			DestTexture.ClearAndResize(width, height);
		}

		public Texture Render(Texture fromTexture, bool final = false)
		{
			if (!final) {
				fbo.Enable();
			}
			Graphics.TheGLTracking.SetShader(shader);
			fromTexture.Enable();
			shader.UniformMatrix("projection", matrix);
			shader.Uniformi("texture", 0);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			bb.Draw();
			if (!final) {
				fbo.Disable();
			}
			return DestTexture;
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
		List<IPostprocStep> steps;
		FramebufferObject fbo;
		Texture fboTexture;

		public PostprocPipeline(int width, int height)
		{
			this.width = (int)SMath.RoundUpToPowerOf2(width);
			this.height = (int)SMath.RoundUpToPowerOf2(height);
			// Sanity checking.
			Log.Assert(Util.IsPowerOf2(this.width));
			Log.Assert(Util.IsPowerOf2(this.height));
			steps = new List<IPostprocStep>();
			fboTexture = new Texture(this.width, this.height);
			fbo = new FramebufferObject(fboTexture);
		}

		public void AddStep(Shader shader)
		{
			var step = new PostprocStep(shader, width, height);
			steps.Add(step);
		}

		public void AddStep(IPostprocStep step)
		{
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
				var resultTexture = step.Render(fromTexture, final: false);
				fromTexture = resultTexture;
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

