using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	public class Camera
	{
		int ScreenW, ScreenH;
		double HardBoundaryFactor = 0.50;
		Actor Target;

		public Camera(Actor target, int screenw, int screenh)
		{
			Target = target;
			ScreenW = screenw;
			ScreenH = screenh;

		}
	}

	public class Affine
	{
		public Affine(Vector2 translation, Vector2 rotation, Vector2 scale)
		{

		}
	}

	public class VBO
	{
		int posBuffer;

		public VBO(float[] vertdata, float[] colors)
		{
			posBuffer = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, posBuffer);
			//GL.BufferData(BufferTarget.ArrayBuffer, vertdata.Length, vertdata, BufferUsageHint.StaticDraw);
		}
	}

	/// <summary>
	/// For now the Camera contains pretty much all the projection and location properties
	/// to set up the OpenGL state.
	/// </summary>
	public class Graphics
	{
		int ScreenW, ScreenH;
		public Matrix4d Projection;
		public Matrix4d Modelview;
		public static double ClipNear = 10.0;
		public static double ClipFar = 20.0;
		public static Vector3d Up = new Vector3d(0.0, 1.0, 0.0);
		public static Vector3d OutOfScreen = new Vector3d(0.0, 0.0, -1.0);
		Shader Shader;

		public Graphics(int screenw, int screenh)
		{
			ScreenW = screenw;
			ScreenH = screenh;

			Shader = new Shader(Shader.DefaultVertShader, Shader.DefaultFragShader);

			InitGL();
		}

		public void InitGL()
		{
			
			GL.EnableClientState((ArrayCap.VertexArray));
			GL.EnableClientState(ArrayCap.NormalArray);
			//GL.EnableClientState(EnableCap.ColorArray);
			GL.EnableClientState(ArrayCap.TextureCoordArray);
			GL.Enable(EnableCap.Texture2D);
			GL.Enable(EnableCap.DepthTest);
			// WTF this annihilates shaders aaaargh
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.ActiveTexture(TextureUnit.Texture1);
			GL.ActiveTexture(TextureUnit.Texture2);
			GL.ClearColor(Color.Blue);
		}

		public void Resize()
		{
			//double fov = Math.PI / 4;
			//double aspectRatio = 4.0 / 3.0;
			Projection = Matrix4d.CreateOrthographic(ScreenW, ScreenH, ClipNear, ClipFar);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref Projection);
		}
		// Does not draw a background
		public void StartDraw()
		{
			/*
			// Coordinate transform between gameworld and screenworld coordinates.
			CameraTarget.X = target.Y;
			CameraTarget.Y = 0.0;
			CameraTarget.Z = target.X;

			// This fixes the light position to (10 units above) the gameworld origin.
			L1.Position.X = (float)-target.X;
			L1.Position.Y = (float)-target.Y;
			L1.Position.Z = (float)-0;
			L1.BindToCurrentShader();

			Vector3d CameraLoc = Vector3d.Multiply(OutOfScreen, CameraDistance);
			CameraLoc = Vector3d.Add(CameraTarget, CameraLoc);
			Modelview = Matrix4d.LookAt(CameraLoc, CameraTarget, Up);
			*/

			// We also need to transform 
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadMatrix(ref Modelview);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			Shader.Enable();

			//GL.BindBuffer(BufferTarget.ArrayBuffer, posBuffer);
			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);

			//GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
			GL.DisableVertexAttribArray(0);
			Shader.Disable();

		}
	}
}

