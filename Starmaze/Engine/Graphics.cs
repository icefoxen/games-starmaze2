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

		int vertexBuffer;
		float[] vertexPositions;

		public Graphics(int screenw, int screenh)
		{
			ScreenW = screenw;
			ScreenH = screenh;

			vertexPositions = new float[]{
				0.75f, 0.75f, 0.0f, 1.0f,
				0.75f, -0.75f, 0.0f, 1.0f,
				-0.75f, -0.05f, 0.0f, 1.0f,
			};

			InitGL();
		}

		public void InitGL()
		{
			vertexBuffer = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertexPositions.Length * sizeof(float)), vertexPositions, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		public void Resize()
		{
		}

		public void StartDraw(Shader shader)
		{
			GL.ClearColor(Color.Green);
			GL.Clear(ClearBufferMask.ColorBufferBit);

			shader.Enable();

			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);

			GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

			GL.DisableVertexAttribArray(0);
			shader.Disable();
		}
	}
}

