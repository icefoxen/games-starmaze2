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

		public ViewManager(double width, double height) : this((float)width, (float)height)
		{
		}

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
			// OPT:
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
		public int Handle;

		/// <summary>
		/// Creates a new texture from the given bitmap.
		/// </summary>
		/// <param name="bitmap">Bitmap.</param>
		public Texture(Bitmap bitmap)
		{
			if (!Util.IsPowerOf2(bitmap.Width) || !Util.IsPowerOf2(bitmap.Height)) {
				// XXX: FormatException isn't really the best here, buuuut...
				// Assertion?  Except I sorta want to check this at runtime in the real game.
				throw new FormatException("Texture sizes must be powers of 2!");
			}
			SetupNewTexture();

			BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
				                  ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			//BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
			//	ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
			/*
			var pixels = new byte[] {
				255, 255, 255, 255,
				255, 0, 0, 255,
				0, 255, 0, 255,
				0, 0, 255, 255,
			};
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 2, 2, 0,
				OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
				*/
			bitmap.UnlockBits(data);

			Disable();
		}

		/// <summary>
		/// Creates a new, empty texture of the given dimensions.
		/// </summary>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		public Texture(int width, int height)
		{
			SetupNewTexture();
			ClearAndResize(width, height);
		}

		public void ClearAndResize(int width, int height)
		{

			if (!Util.IsPowerOf2(width) || !Util.IsPowerOf2(height)) {
				// XXX: FormatException isn't really the best here, buuuut...
				// Assertion?  Except I sorta want to check this at runtime in the real game.
				throw new FormatException("Texture sizes must be powers of 2!");
			}

			Enable();
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, 
				OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			Disable();
		}

		/// <summary>
		/// Actually creates the texture object and sets the appropriate parameters.
		/// 
		/// Note that this leaves the current texture enabled, because it's for internal use only.
		/// </summary>
		// XXX: Right now, texture parameters are basically hardcoded.  That's fine as long as we never want
		// anything different!
		void SetupNewTexture()
		{
			Handle = GL.GenTexture();
			Enable();

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
		}

		public void Enable()
		{
			GL.Enable(EnableCap.Texture2D);
			GL.ActiveTexture(TextureUnit.Texture0);
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
			if (GraphicsContext.CurrentContext != null) {
				GL.DeleteTexture(Handle);
			}
		}
	}

	public class VertexMember
	{
		public readonly string Name;
		public readonly int Count;
		public const int ElementBytes = sizeof(float);

		public VertexMember(string name, int count)
		{
			Name = name;
			Count = count;
		}
	}


	public class VertexLayout
	{
		public readonly VertexMember[] Members;
		public readonly int ElementCount;
		public readonly int ByteCount;

		public static readonly VertexLayout ColorVertex = 
			new VertexLayout(new VertexMember[] {
				new VertexMember("position", 2),
				new VertexMember("color", 4)
			});

		public static readonly VertexLayout TextureVertex = 
			new VertexLayout(new VertexMember[] {
				new VertexMember("position", 2),
				new VertexMember("color", 4),
				new VertexMember("texcoord", 2),
			});

		public VertexLayout(VertexMember[] members)
		{
			Members = members;
			ElementCount = 0;
			foreach (var m in members) {
				ElementCount += m.Count;
			}
			ByteCount = ElementCount * VertexMember.ElementBytes;
		}

		int GetElementOffset(string name)
		{
			int byteOffset = 0;
			for (int i = 0; i < Members.Length; i++) {
				if (Members[i].Name == name) {
					return byteOffset;
				}
				byteOffset += Members[i].Count * VertexMember.ElementBytes;
			}
			var msg = String.Format("Tried to get vertex member offset of {0} which isn't here", name);
			Log.Message(msg);
			throw new KeyNotFoundException(msg);
		}
	}

	/* Honestly something like this is how I imagined VertexList's working in the first place.
	 * But I'm sick of messing with things that work, and I'm not entirely sure how the type effects
	 * would propegate out to shaders and such, so I'm just gonna leave this here and 
	 * move on.
	public struct Vertex1
	{
		public Vector2 Pos;
		public Color4 Color;
	}

	public struct Vertex2
	{
		public Vector2 Pos;
		public Color4 Color;
		public Vector2 Texcoord;
	}

	public class VertexList2<T>
	{
		public List<T> Verts;
		public readonly VertexLayout Layout;

		public VertexList2(VertexLayout l)
		{
			Layout = l;
		}
	}

	public class VertexList3 : VertexList2<Vertex1>
	{
		public VertexList3() : base(VertexLayout.ColorVertex)
		{
		}

	}
	*/


	/// <summary>
	/// Represents a collection of vertices of a particular type.
	/// On its own, does nothing, just had a bunch of floats and a layout description for a VertexArray
	/// to read in.
	/// </summary>
	public class VertexList
	{
		public readonly VertexLayout Layout;
		List<float> Vertexes;

		public int LengthInVertexes {
			get {
				// Sanity check.
				Log.Assert(Vertexes.Count % Layout.ElementCount == 0, 
					"Failed sanity check for number of vertex elements, have {0} extra",
					Vertexes.Count % Layout.ElementCount
				);
				return Vertexes.Count / Layout.ElementCount;
			}
		}

		public int LengthInElements {
			get {
				return Vertexes.Count;
			}
		}

		public int LengthInBytes {
			get {
				return Vertexes.Count * VertexMember.ElementBytes;
			}
		}

		public VertexList(VertexLayout layout)
		{
			Layout = layout;
			Vertexes = new List<float>();
		}

		public void AddVertex(float[] vert)
		{
			Log.Assert(vert.Length == Layout.ElementCount, 
				"Got {0} elements for vertex, expected {1}", vert.Length, Layout.ElementCount);
			Vertexes.AddRange(vert);
		}

		public void AddVertexes(float[] verts)
		{
			var numVerts = verts.Length / Layout.ElementCount;
			var leftoverVerts = verts.Length % Layout.ElementCount;
			Log.Assert(leftoverVerts == 0,
				"Got {0} elements, that makes {1} vertexes, but has {2} elements left over",
				verts.Length, numVerts, leftoverVerts
			);

			Vertexes.AddRange(verts);
		}

		// Convenience functions for creating common vertex types.
		public void AddColorVertex(Vector2 pos, Color4 color)
		{
			Log.Assert(Layout == VertexLayout.ColorVertex, "Attempted to add a color vertex to a VertexList of the wrong type!");
			var elementCount = Vertexes.Count;
			Vertexes.Add(pos.X);
			Vertexes.Add(pos.Y);
			Vertexes.Add(color.R);
			Vertexes.Add(color.G);
			Vertexes.Add(color.B);
			Vertexes.Add(color.A);
			// This assertion is to help catch things if we change the vertex format but don't update this function.
			var elementsAdded = Vertexes.Count - elementCount;
			Log.Assert(elementsAdded == (VertexLayout.ColorVertex.ElementCount));
		}

		public void AddTextureVertex(Vector2 pos, Color4 color, Vector2 texcoord)
		{
			Log.Assert(Layout == VertexLayout.TextureVertex, "Attempted to add a color vertex to a VertexList of the wrong type!");
			var elementCount = Vertexes.Count;
			Vertexes.Add(pos.X);
			Vertexes.Add(pos.Y);
			Vertexes.Add(color.R);
			Vertexes.Add(color.G);
			Vertexes.Add(color.B);
			Vertexes.Add(color.A);
			Vertexes.Add(texcoord.X);
			Vertexes.Add(texcoord.Y);
			// This assertion is to help catch things if we change the vertex format but don't update this function.
			var elementsAdded = Vertexes.Count - elementCount;
			Log.Assert(elementsAdded == (VertexLayout.TextureVertex.ElementCount));
		}

		public float[] ToArray()
		{
			return Vertexes.ToArray();
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
	public class VertexArray : IDisposable
	{
		VertexList Vertexes;
		VertexLayout Layout;
		IList<uint> indices;
		int vao;
		int buffer;
		int indexBuffer;
		BufferUsageHint usageHint;
		PrimitiveType primitive;
		int NumberOfIndices;

		public VertexArray(Shader shader,
		                   VertexList vertexes,
		                   IList<uint> idxs = null,
		                   PrimitiveType prim = PrimitiveType.Triangles, 
		                   BufferUsageHint usage = BufferUsageHint.StaticDraw)
		{
			Log.Assert(shader != null);
			Log.Assert(vertexes != null);

			Vertexes = vertexes;
			Layout = vertexes.Layout;
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
			AddVertexesToBuffer(vertexes);
			AddIndicesToBuffer(indices);
			SetupVertexPointers(shader, vertexes);
			// Unbinding the buffer *does not* alter the state of the vertex array object.
			// The association between buffer and vao is made on the GL.VertexAttribPointer() call.
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			// Except for ElementArrayBuffer's, OF COURSE.(?)
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
			if (GraphicsContext.CurrentContext != null) {
				GL.DeleteVertexArray(vao);
				GL.DeleteBuffer(buffer);
			}
		}

		public void Dispose()
		{
			Dispose(true);
			// Don't run the finalizer, since it's a waste of time.
			GC.SuppressFinalize(this);
		}

		uint[] generateLinearIndices(int count)
		{
			var newIndices = new uint[count];
			for (int i = 0; i < count; i++) {
				newIndices[i] = (uint)i;
			}
			return newIndices;
		}

		void AddVertexesToBuffer(VertexList verts)
		{
			Log.Assert(verts != null);
			// Not the fastest way, but the easiest.
			var vertexData = verts.ToArray();
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(verts.LengthInBytes),
				vertexData, usageHint);
		}

		void AddIndicesToBuffer(IList<uint> indices)
		{
			Log.Assert(indices != null);
			var indexArray = new uint[indices.Count];
			// IList doesn't have ToArray, irritatingly.
			indices.CopyTo(indexArray, 0);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indexArray.Length * sizeof(int)),
				indexArray, BufferUsageHint.StaticRead);
		}

		void SetupVertexPointers(Shader shader, VertexList vertexes)
		{
			Log.Assert(shader != null);
			Log.Assert(vertexes != null);

			var byteOffset = 0;
			foreach (var vertexMember in Layout.Members) {
				var location = shader.VertexAttributeLocation(vertexMember.Name);
				GL.EnableVertexAttribArray(location);
				GL.VertexAttribPointer(location, vertexMember.Count, VertexAttribPointerType.Float, 
					false, Layout.ByteCount, byteOffset);
				byteOffset += vertexMember.Count * VertexMember.ElementBytes;
			}
		}

		public void Draw()
		{
			//Console.WriteLine("binding vao...");
			GL.BindVertexArray(vao);
			//Console.WriteLine("Trying to draw...");
			GL.DrawElements(primitive, NumberOfIndices, DrawElementsType.UnsignedInt, 0);
			//Console.WriteLine("Done drawing...");
			GL.BindVertexArray(0);
		}
	}

	/// <summary>
	/// A set of OpenGL parameters.
	/// </summary>
	public class GLDiscipline
	{
		public static readonly GLDiscipline DEFAULT = new GLDiscipline(
			                                              new Tuple<BlendingFactorSrc, BlendingFactorDest>(
				                                              BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha)
		                                              );
		// XXX: The tuple here is a little janky.
		Tuple<BlendingFactorSrc, BlendingFactorDest> blendFunc;

		public GLDiscipline(Tuple<BlendingFactorSrc, BlendingFactorDest> blendfunc = null, HintMode PerspectiveCorrectionHint = HintMode.Nicest)
		{
			blendFunc = blendfunc;
		}

		public void ForceApply()
		{
			if (blendFunc != null) {
				Console.WriteLine("Enabling blending");
				GL.Enable(EnableCap.Blend);
				GL.BlendFunc(blendFunc.Item1, blendFunc.Item2);
			} else {
				Console.WriteLine("Disabling blending");
				GL.Disable(EnableCap.Blend);
			}
		}
	}

	public class GLHint
	{
		public static readonly GLHint DEFAULT = new GLHint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
		List<Tuple<HintTarget, HintMode>> hints;

		public GLHint(IEnumerable<Tuple<HintTarget, HintMode>> hints)
		{
			hints = new List<Tuple<HintTarget, HintMode>>(hints);
		}

		public GLHint(HintTarget target, HintMode hint)
		{
			hints = new List<Tuple<HintTarget, HintMode>>();
			AddHint(target, hint);
		}

		public void AddHint(HintTarget target, HintMode hint)
		{
			hints.Add(new Tuple<HintTarget, HintMode>(target, hint));
		}

		public void Apply()
		{
			foreach (var h in hints) {
				var target = h.Item1;
				var hint = h.Item2;
				GL.Hint(target, hint);
			}
		}
	}

	/// <summary>
	/// An object that tracks and is used to set the current OpenGL state.
	/// </summary>
	public class GLTracking
	{
		GLDiscipline discipline;
		GLHint hint;
		Shader shader;

		public GLTracking()
		{
		}

		public void SetHint(GLHint hint)
		{
			if (this.hint == hint) {
				return;
			} else {
				hint.Apply();
				this.hint = hint;
			}
		}

		public void SetDiscipline(GLDiscipline discipline)
		{
			if (discipline == this.discipline) {
				return;
			} else {
				discipline.ForceApply();
				this.discipline = discipline;
			}
		}

		public void SetShader(Shader shader)
		{
			if (shader == this.shader) {
				return;
			} else if (shader == null) {
				GL.UseProgram(0);
			} else {
				shader.Enable();
				this.shader = shader;
			}
		}
	}

	/// <summary>
	/// Functions for handily setting up OpenGL state.
	/// </summary>
	public static class Graphics
	{
		// Singleton pattern here, sigh.
		static GLTracking _theGLTracking;

		public static void InitGLTracking()
		{
			_theGLTracking = new GLTracking();
		}

		public static GLTracking TheGLTracking {
			get {
				return _theGLTracking;
			}
		}

		public static string GetGLInfo()
		{
			var version = GL.GetString(StringName.Version);
			var vendor = GL.GetString(StringName.Vendor);
			var renderer = GL.GetString(StringName.Renderer);
			var glslVersion = GL.GetString(StringName.ShadingLanguageVersion);

			return String.Format("Using OpenGL version {0} from {1}, renderer {2}, GLSL version {3}", version, vendor, renderer, glslVersion);
		}

		public static void Init()
		{
			Log.Message(GetGLInfo());
			Graphics.InitGLTracking();
		}

		public static void ClearScreen()
		{
			GL.ClearColor(Color4.Black);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}
	}

	/// <summary>
	/// An object that specifies to render the scene to the given texture, with the given shader.
	/// </summary>
	public class FramebufferObject : IDisposable
	{
		int Handle;
		Texture destination;

		public FramebufferObject(Texture dest)
		{
			destination = dest;
			// Create frame buffer
			Handle = GL.GenFramebuffer();
			Enable();
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2D, destination.Handle, 0);
			var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
			// TODO: More rigorous error checking?
			Log.Assert(status == FramebufferErrorCode.FramebufferComplete, 
				"Something went wrong creating framebuffer for postprocessing step: {0}", status);
			Disable();
		}

		public void Enable()
		{
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
		}

		public void Disable()
		{
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		}

		private bool disposed = false;

		~FramebufferObject()
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
			// We don't clean up the texture because it might still be used by something else.
			// Seems like reference counting would be nice for that...
			if (GraphicsContext.CurrentContext != null) {
				GL.DeleteFramebuffer(Handle);
			}
		}
	}

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
			width = screenw;
			height = screenh;
			this.shader = shader;
			matrix = Matrix4.CreateOrthographic(12, -8, -1f, 10f);

			tex = Resources.TheResources.GetTexture("playertest");

			/*
            var pixels = new byte[] {
                255, 255, 255, 255,
                255, 0, 0, 255,
                0, 255, 0, 255,
                0, 0, 255, 255,
            };
			tex.Enable();
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 2, 2, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
			tex.Disable();
			*/

			// Create back-buffer
			// XXX: Is ActiveTexture needed?  I THINK so...
			//GL.ActiveTexture(TextureUnit.Texture0);
			DestTexture = new Texture(width, height);

			// Create framebuffer
			fbo = new FramebufferObject(DestTexture);
			this.bb = Starmaze.Content.Images.Billboard();
			/*
			// Make a billboard to render to.
			// XXX: Aspect ratio...
			var aspectRatio = 4.0f / 3.0f;
			var bb = new VertexList(VertexLayout.TextureVertex);

			var halfHeight = 1.0f / 2;
			var halfWidth = 1.0f / 2;

			bb.AddTextureVertex(
				new Vector2(-halfWidth, -halfHeight),
				Color4.White,
				new Vector2(0, (1.0f / aspectRatio))
			);
			bb.AddTextureVertex(
				new Vector2(-halfWidth, halfHeight),
				Color4.White,
				new Vector2(0, 0)
			);
			bb.AddTextureVertex(
				new Vector2(halfWidth, halfHeight),
				Color4.White,
				new Vector2(1, 0)
			);
			bb.AddTextureVertex(
				new Vector2(halfWidth, -halfHeight),
				Color4.White,
				new Vector2(1, (1.0f / aspectRatio))
			);
			var indices = new uint[] {
				0, 1, 2,
				0, 2, 3,
			};
			this.bb = new VertexArray(shader, bb, idxs: indices);
			*/
		}

		/// <summary>
		/// Rescale frame buffer object.
		/// </summary>
		/// <param name="screenw">Screen width.</param>
		/// <param name="screenh">Screen height.</param>
		public void Reshape(int screenw, int screenh)
		{
			width = screenw;
			height = screenh;
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
		List<PostprocStep> steps;
		FramebufferObject fbo;
		Texture fromTexture;

		public PostprocPipeline()
		{
			steps = new List<PostprocStep>();
			fromTexture = new Texture(1024, 1024);
			fbo = new FramebufferObject(fromTexture);
		}

		public void AddStep(Shader shader)
		{
			var step = new PostprocStep(shader, 1024, 1024);
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
			GL.ClearColor(Color4.Tan);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			drawScene();
			fbo.Disable();

			// Then we take the drawn scene and run it through each of the postprocessing steps.
			for (int i = 0; i < steps.Count - 2; i++) {
				var step = steps[i];
				step.Render(fromTexture, final: false);
				fromTexture = step.DestTexture;
			}
			// Then do the final rendering to the screen.
			steps[steps.Count - 1].Render(fromTexture, final: true);
		}
	}
}

