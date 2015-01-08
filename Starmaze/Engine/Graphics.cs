using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK;
using OpenTK.Graphics;
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

	/// <summary>
	/// A coordinate transform.  Performs low-level translation, rotation, etc.
	/// Rotation is clockwise.
	/// </summary>
	public struct Transform
	{
		public Vector2 Translation;
		public float Rotation;
		public Vector2 Scale;

		public Transform(Vector2 trans, float rot, Vector2 scale)
		{
			Translation = trans;
			Rotation = rot;
			Scale = scale;
		}

		public Transform(Vector2 trans, float rot) : this(trans, rot, Vector2.One)
		{

		}

		public Transform(Vector2 trans) : this(trans, 0, Vector2.One)
		{

		}

		public Transform(float rot) : this(Vector2.Zero, rot, Vector2.One)
		{
		}

		/// <summary>
		/// Applies the Transform to the given matrix and returns a new one.
		/// </summary>
		/// <returns>The matrix.</returns>
		/// <param name="matrix">matrix</param>
		public Matrix4 TransformMatrix(Matrix4 matrix)
		{
			// This might be done more efficiently without creating a bunch of matrices and doing lots
			// of multiplications, but, for now, we do it the way that involves fewer headaches.
			var translationMatrix = Matrix4.CreateTranslation(Translation.X, Translation.Y, 0.0f);
			var rotationMatrix = Matrix4.CreateRotationZ(-Rotation);
			var scaleMatrix = Matrix4.CreateScale(Scale.X, Scale.Y, 0.0f);
			// Remember order is important here!
			var transformMatrix = scaleMatrix * (rotationMatrix * translationMatrix);
			return transformMatrix * matrix;
		}
	}

	/// <summary>
	/// A texture.
	/// It's small, but it's _not_ a struct, because it allocates resources and thus needs
	/// the reference semantics of a real object.
	/// </summary>
	//  We could make it a value type but that would require making
	// an object to manage them beyond the ResourceLoader or making the ResourceLoader do more work,
	// so.
	public class Texture : IDisposable
	{
		int Handle;

		public Texture(Bitmap bitmap)
		{
			if (!Util.IsPowerOf2(bitmap.Width) || !Util.IsPowerOf2(bitmap.Height)) {
				// XXX: FormatException isn't really the best here, buuuut...
				throw new FormatException("Texture sizes must be powers of 2!");
			}
			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

			Handle = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, Handle);

			BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
			                                  ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
			              OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
			bitmap.UnlockBits(data);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		public void Enable()
		{
			GL.BindTexture(TextureTarget.Texture2D, Handle);
		}

		public void Disable()
		{
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		private bool disposed = false;

		~Texture()
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
			}
			// Clean up unmanaged resources
			GL.DeleteTexture(Handle);
		}
	}

	/// <summary>
	/// Represents an array of a single vertex attribute type.
	/// On its own, does nothing apart from hold data.
	/// </summary>
	public class VertexAttributeArray
	{
		public float[] Data;
		public int ElementsPerVertex;
		public const int SizeOfElement = sizeof(float);
		public string Name;

		public VertexAttributeArray(string name, float[] data, int elementsPerVertex)
		{
			Log.Assert(name != null);
			Log.Assert(data != null);
			Log.Assert(elementsPerVertex > 0);
			Name = name;
			Data = data;
			ElementsPerVertex = elementsPerVertex;
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
	// the shaders all include a common header file, buuuuut...
	// XXX: It might be easier to just have a 'vertex' type for each _sort_ of thing we want
	// to put together, and make this able to load the things in and interleave them properly
	// and stuff...  but then one starts worrying about packing and stuff like that.
	// Some reflection might make it easier.
	// It might be better to have each Vertex be composed of multiple VertexAttributes, which can
	// then be fed into this in interleaved order.
	// TODO: Make this indexed?  Even simply indexed, it would lay the path for properly indexed
	// stuff in the future.  If we ever want to load meshes from files, for instance.
	public class VertexArray : IDisposable
	{
		IEnumerable<VertexAttributeArray> AttributeLists;
		IList<uint> indices;
		int vao;
		int buffer;
		int indexBuffer;
		BufferUsageHint usageHint;
		PrimitiveType primitive;
		int NumberOfIndices;

		public VertexArray(Shader shader, 
		                   IEnumerable<VertexAttributeArray> attrs, 
		                   IList<uint> idxs = null,
		                   PrimitiveType prim = PrimitiveType.Triangles, 
		                   BufferUsageHint usage = BufferUsageHint.StaticDraw)
		{
			Log.Assert(shader != null);
			Log.Assert(attrs != null);

			var vertexCount = checkVertexArrays(attrs);
			if (idxs == null) {
				idxs = generateLinearIndices(vertexCount);
			}
			AttributeLists = attrs;
			indices = idxs;
			usageHint = usage;
			primitive = prim;
			NumberOfIndices = indices.Count;
			vao = GL.GenVertexArray();
			GL.BindVertexArray(vao);
			buffer = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
			indexBuffer = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
			AddAttributesToBuffer(attrs);
			AddIndicesToBuffer(indices);
			SetupVertexPointers(shader, attrs);
			// Unbinding the buffer *does not* alter the state of the vertex array object.
			// The association between buffer and vao is made on the GL.VertexAttribPointer() call.
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			// Except for ElementArrayBuffer's, OF COURSE.
			//GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			GL.BindVertexArray(0);
		}
		// Implementing tedious disposal-tracking semantics, see
		// http://gregbee.ch/blog/implementing-and-using-the-idisposable-interface
		// and
		// http://msdn.microsoft.com/en-us/library/system.idisposable%28v=vs.110%29.aspx
		private bool disposed = false;

		~VertexArray()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed) {
				return;
			}
			disposed = true;
			if (disposing) {
				// Clean up managed resources
				// This bit is only here in the unlikely event we need to do stuff
				// in the finalizer or override this in a child class or something, I guess.
				// But resource allocation is Important and Hard so I'm cleaving to the
				// recommended idiom in this.
			}
			// Clean up unmanaged resources
			// BUGGO: On the other hand, these calls crash the program whenever they happen, so.
			// We just need to take out the test code in TestRenderer and only ever create these through
			// the ResourceLoader, so.
			GL.DeleteVertexArray(vao);
			GL.DeleteBuffer(buffer);
		}

		public void Dispose()
		{
			Dispose(true);
			// Don't run the finalizer, since it's a waste of time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Verifies that all the vertex attribute arrays are of the same size
		/// </summary>
		/// <param name="attr">Attr.</param>
		int checkVertexArrays(IEnumerable<VertexAttributeArray> attrs)
		{
			int vertexCount = 0;
			foreach (var attr in attrs) {
				var length = attr.LengthInElements();
				// BUGGO: This is actually hugely misguided; LengthInElements isn't necessarily valid
				// when we're using indexed drawing.
				//Log.Warn(vertexCount != 0 && length != vertexCount, "VertexAttributeArray's have different lengths");
			}
			return vertexCount;
		}

		uint[] generateLinearIndices(int count)
		{
			var newIndices = new uint[count];
			for (int i = 0; i < count; i++) {
				newIndices[i] = (uint)i;
			}
			return newIndices;
		}
		/*
		 * This is old but verifying that the number of verts in all buffers is the same might be
		 * a good idea anyway.
		int  GetVertCount(IEnumerable<VertexAttributeArray> attrs)
		{
			var vertCount = int.MaxValue;
			foreach (var attr in attrs) {
				var totalVerts = attr.LengthInElements() / attr.ElementsPerVertex;
				// BUGGO: Make this warning work.
				//Util.Warn(vertCount != int.MaxValue && totalVerts != vertCount, "Inititalizing VertexArray with different size attributes");
				//Console.WriteLine("Length: {0}, eltsPerVert: {1}, total: {2}, vertCount: {3}", attr.LengthInElements(), attr.ElementsPerVertex, totalVerts, vertCount);
				// We want to draw the minimum number of vertices we have all the data for.
				vertCount = Math.Min(totalVerts, vertCount);
			}
			return vertCount;
		}
		*/
		void AddAttributesToBuffer(IEnumerable<VertexAttributeArray> attrs)
		{
			Log.Assert(attrs != null);
			// Not the fastest way, but the easiest.
			var accm = new List<float>();
			foreach (var attr in attrs) {
				accm.AddRange(attr.Data);
			}
			var allAttrs = accm.ToArray();
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(allAttrs.Length * VertexAttributeArray.SizeOfElement),
			              allAttrs, usageHint);
		}

		void AddIndicesToBuffer(IList<uint> indices)
		{
			Log.Assert(indices != null);
			var indexArray = new uint[indices.Count];
			// OPT: If we were stricter we might be able to get rid of this copy
			indices.CopyTo(indexArray, 0);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indexArray.Length * sizeof(int)),
			              indexArray, BufferUsageHint.StaticRead);
		}

		void SetupVertexPointers(Shader shader, IEnumerable<VertexAttributeArray> attrs)
		{
			Log.Assert(shader != null);
			Log.Assert(attrs != null);
			var byteOffset = 0;
			foreach (var attr in attrs) {
				var location = shader.VertexAttributeLocation(attr.Name);
				GL.EnableVertexAttribArray(location);
				GL.VertexAttribPointer(location, attr.ElementsPerVertex, VertexAttribPointerType.Float,
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
			// XXX: DrawElementType.Short might be better, but then we have to check and make sure everything's
			// within reach of a short...
			GL.DrawElements(primitive, NumberOfIndices, DrawElementsType.UnsignedInt, 0);
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
			var c = Color4.AliceBlue;
			GL.ClearColor(Color4.Gray);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}

		public static void FinishDraw()
		{
		}

		public static readonly Color4 DEFAULT_COLOR = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
		public const double DEFAULT_STROKE_WIDTH = 2.0;
	}
}

