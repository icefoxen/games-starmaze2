using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	/// <summary>
	/// Stores and defines the projection matrix, which sets the size of the world view, where the "camera"
	/// is looking, and so on.
	/// </summary>
	public class ViewManager
	{
		public Vector2 VisibleSize;
		public Matrix4 ProjectionMatrix;
		public float ZNear;
		public float ZFar;

		public ViewManager(float width, float height)
		{
			VisibleSize = new Vector2(width, height);
			// XXX: Right now these values are pretty arbitrary.
			ZNear = 0.0f;
			ZFar = 10.0f;
			ProjectionMatrix = Matrix4.CreateOrthographic(width, height, ZNear, ZFar);
		}

		public void Translate(Vector2 location)
		{
			Translate(location.X, location.Y);
		}

		public void Translate(float x, float y)
		{
			var translation = Matrix4.CreateTranslation(new Vector3(x, y, 0.0f));
			ProjectionMatrix = ProjectionMatrix * translation;
		}

		public void CenterOn(float x, float y)
		{
			var halfWidth = VisibleSize.X / 2;
			var halfHeight = VisibleSize.Y / 2;
			ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(x - halfWidth, x + halfWidth,
			                                                       y - halfHeight, y + halfHeight,
			                                                       ZNear, ZFar);
		}
	}
	/*
	/// <summary>
	/// A coordinate transform.  Can have a parent, at which point it specifies a position
	/// relative to its parent.  Useful for animation as well as specifying anchor points for
	/// things like particle effects or weapon firing points.
	/// </summary>
	public class PositionNode
	{
		public Vector2d Position;
		public double Rotation;
		public PositionNode Parent;
	}
	*/
	/// <summary>
	/// Represents an array of a single vertex attribute type.
	/// On its own, does nothing apart from hold data.
	/// </summary>
	public class VertexAttributeArray
	{
		public float[] Data;
		public int CountPerVertex;
		public const int SizeOfElement = sizeof(float);
		public string Name;

		public VertexAttributeArray(string name, float[] data, int countPerVertex)
		{
			Name = name;
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
	/// 
	/// Note that this process is specific to a particular shader, since it has to know where
	/// the shader's inputs are to put the right vertex data in the right place.  The alternative
	/// is having some convention so that position data is always location 0, color data is always
	/// location 1, and so on, but then we have to ensure it's identical across all shaders, and they
	/// all follow the same convention, and the first time we'll know something is wrong is when it crashes
	/// or draws corrupt.  SO, we'll do it this way, and have it check for us that vertex attribute locations
	/// match with the shader correctly.
	/// </summary>
	// XXX: Making all the shader variables uniform could be done easily by making 
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

		public VertexArray(Shader shader, VertexAttributeArray[] attrs) : this(shader, attrs, BufferUsageHint.StaticDraw)
		{
		}

		public VertexArray(Shader shader, VertexAttributeArray[] attrs, BufferUsageHint usage)
		{
			AttributeLists = attrs;
			usageHint = usage;
			vao = GL.GenVertexArray();
			GL.BindVertexArray(vao);
			buffer = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
			AddAttributesToBuffer(attrs);
			SetupVertexPointers(shader, attrs);
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

		void SetupVertexPointers(Shader shader, VertexAttributeArray[] attrs)
		{
			var byteOffset = 0;
			foreach (var attr in attrs) {
				var location = shader.VertexAttributeLocation(attr.Name);
				GL.EnableVertexAttribArray(location);
				GL.VertexAttribPointer(location, attr.CountPerVertex, VertexAttribPointerType.Float,
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
	/// Functions for handily setting up OpenGL state.
	/// </summary>
	public static class Graphics
	{
		public static string GetGLInfo()
		{
			var version = GL.GetString(StringName.Version);
			var vendor = GL.GetString(StringName.Vendor);
			var renderer = GL.GetString(StringName.Renderer);
			var glslVersion = GL.GetString(StringName.ShadingLanguageVersion);

			return String.Format("Using OpenGL version {0} from {1}, renderer {2}, GLSL version {3}", version, vendor, renderer, glslVersion);
		}

		public static void InitGL()
		{
			GL.Enable(EnableCap.DepthTest);
			GL.DepthMask(true);
			GL.DepthFunc(DepthFunction.Lequal);
			GL.DepthRange(0.0f, 1.0f);
			Console.WriteLine(GetGLInfo());
		}

		public static void StartDraw()
		{
			GL.ClearColor(Color.Gray);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}

		public static void FinishDraw()
		{

		}
	}
}

