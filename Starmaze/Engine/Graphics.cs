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
	public class Graphics : IDisposable
	{
		int ScreenW, ScreenH;
		public Matrix4d Projection;
		public Matrix4d Modelview;
		public static double ClipNear = 10.0;
		public static double ClipFar = 20.0;
		public static Vector3d Up = new Vector3d(0.0, 1.0, 0.0);
		public static Vector3d OutOfScreen = new Vector3d(0.0, 0.0, -1.0);

		Matrix4 projectionMatrix;
		int vao;
		int vertexBuffer;
		float[] vertexData;

		public Graphics(int screenw, int screenh)
		{
			ScreenW = screenw;
			ScreenH = screenh;

			vertexData = new float[]{
				// Verts
				0.0f,    0.5f, 0.0f, 1.0f,
				0.5f, -0.366f, 0.0f, 1.0f,
				-0.5f, -0.366f, 0.0f, 1.0f,

				// Colors
				1.0f,    0.0f, 0.0f, 1.0f,
				0.0f,    1.0f, 0.0f, 1.0f,
				0.0f,    0.0f, 1.0f, 1.0f,
			};

			InitGL();
		}
		public string GetGLInfo() {
			var version = GL.GetString(StringName.Version);
			var vendor = GL.GetString(StringName.Vendor);
			var renderer = GL.GetString(StringName.Renderer);
			var glslVersion = GL.GetString(StringName.ShadingLanguageVersion);

			return String.Format("Using OpenGL version {0} from {1}, renderer {2}, GLSL version {3}", version, vendor, renderer, glslVersion);
		}
		public void InitGL()
		{
			GL.Enable(EnableCap.DepthTest);
			GL.DepthMask(true);
			GL.DepthFunc(DepthFunction.Lequal);
			GL.DepthRange(0.0f, 1.0f);
			Console.WriteLine(GetGLInfo());
			vertexBuffer = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertexData.Length * sizeof(float)), vertexData, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			vao = GL.GenVertexArray();
			GL.BindVertexArray(vao);

			projectionMatrix = Matrix4.CreateOrthographicOffCenter(-3, 20, -3, 20, 0, 10);
		}

		public void Dispose() {
			GL.DeleteVertexArray(vao);
			GL.DeleteBuffer(vertexBuffer);
		}

		public void Resize()
		{
		}

		public void StartDraw(Shader shader)
		{
			GL.ClearColor(Color.Gray);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			shader.Enable();
			shader.UniformMatrix("projection", projectionMatrix);

			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			// Vertex data 
			GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 0, 0);
			// Color data
			GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, 48);

			GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

			GL.DisableVertexAttribArray(0);
			GL.DisableVertexAttribArray(1);
			shader.Disable();
		}
	}
}

