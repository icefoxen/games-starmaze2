using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	/*
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
	*/
	/// <summary>
	/// A coordinate transform.  Can have a parent, at which point it specifies a position
	/// relative to its parent.  Useful for animation as well as specifying anchor points for
	/// things like particle effects or weapon firing points.
	/// </summary>
	public class PositionNode
	{
		public Matrix2d Position;
		public double Rotation;
		public PositionNode Parent;
	}

	/// <summary>
	/// Represents an array of a single vertex attribute.
	/// On its own, does nothing apart from hold data.
	/// </summary>
	public class VertexAttributeArray
	{
		public float[] Data;
		public int CountPerVertex;
		public const int SizeOfElement = sizeof(float);

		public VertexAttributeArray(float[] data, int countPerVertex)
		{
			Data = data;
			CountPerVertex = countPerVertex;
		}

		public int LengthInElements()
		{
			return Data.Length;
		}
	}

	/// <summary>
	/// Contains one or more VertexAttributeArray's, shoves them into OpenGL memory,
	/// and draws them.
	/// </summary>
	// XXX: It might be easier to just have a 'vertex' type for each _sort_ of thing we want
	// to put together, and make this able to load the things in and interleave them properly
	// and stuff...  but then one starts worrying about packing and stuff like that.
	// Some reflection might make it easier.
	// It might be better to have each Vertex be composed of multiple VertexAttributes, which can
	// then be fed into this in interleaved order.
	// XXX: It might be nicer to associate vertex attributes with names, but for now,
	// we don't do that.
	public class VertexArray
	{
		VertexAttributeArray[] AttributeLists;
		int vao;
		int buffer;
		BufferUsageHint usageHint;

		public VertexArray(VertexAttributeArray[] attrs) : this(attrs, BufferUsageHint.StaticDraw)
		{

		}

		public VertexArray(VertexAttributeArray[] attrs, BufferUsageHint usage)
		{
			AttributeLists = attrs;
			usageHint = usage;
			vao = GL.GenVertexArray();
			GL.BindVertexArray(vao);
			buffer = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
			AddAttributesToBuffer(attrs);
			SetupVertexPointers(attrs);
			// Unbinding the buffer *does not* alter the state of the vertex array object.
			// The association is made on the GL.VertexAttribPointer() call.
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindVertexArray(0);
		}

		void AddAttributesToBuffer(VertexAttributeArray[] attrs)
		{
			// Not the fastest way, but the easiest.
			var accm = new List<float>();
			foreach (var attr in attrs) {
				accm.AddRange(attr.Data);
			}
			var allAttrs = accm.ToArray();
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(allAttrs.Length * VertexAttributeArray.SizeOfElement),
			              allAttrs, usageHint);
		}

		void SetupVertexPointers(VertexAttributeArray[] attrs)
		{
			var byteOffset = 0;
			for (int i = 0; i < attrs.Length; i++) {
				var attr = attrs[i];
				GL.EnableVertexAttribArray(i);
				GL.VertexAttribPointer(i, attr.CountPerVertex, VertexAttribPointerType.Float,
				                       false, 0, byteOffset);
				byteOffset += attr.LengthInElements() * VertexAttributeArray.SizeOfElement;
			}
		}

		int TotalDataLengthInElements()
		{
			var total = 0;
			foreach (var a in AttributeLists) {
				total += a.LengthInElements();
			}
			return total;
		}

		public void Draw()
		{
			GL.BindVertexArray(vao);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
			GL.BindVertexArray(0);
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
		int vertexBuffer;
		VertexArray verts;

		public Graphics(int screenw, int screenh)
		{
			ScreenW = screenw;
			ScreenH = screenh;

			var vertexData = new float[] {
				// Verts
				0.0f, 0.5f, 0.0f,
				0.5f, -0.366f, 0.0f,
				-0.5f, -0.366f, 0.0f,
			};
			var colorData = new float[] {
				// Colors
				1.0f, 0.0f, 0.0f, 1.0f,
				0.0f, 1.0f, 0.0f, 1.0f,
				0.0f, 0.0f, 1.0f, 1.0f,
			};

			InitGL();

			var v = new VertexAttributeArray()[] {
				new VertexAttributeArray(vertexData, 3),
				new VertexAttributeArray(colorData, 4)
			};
			verts = new VertexArray(v);

		}

		public string GetGLInfo()
		{
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


			projectionMatrix = Matrix4.CreateOrthographicOffCenter(-3, 20, -3, 20, 0, 10);
		}

		public void Dispose()
		{
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
			verts.Draw();
			//mesh.Draw();

			/*
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
			*/
			shader.Disable();
		}
	}
}

