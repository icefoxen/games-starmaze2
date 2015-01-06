using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			Debug.Assert(name != null);
			Debug.Assert(data != null);
			Debug.Assert(elementsPerVertex > 0);
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
	public class VertexArray : IDisposable
	{
		VertexAttributeArray[] AttributeLists;
		int vao;
		int buffer;
		BufferUsageHint usageHint;
		PrimitiveType primitive;
		int NumberOfVerts;

		public VertexArray(Shader shader, 
		                   VertexAttributeArray[] attrs, 
		                   PrimitiveType prim = PrimitiveType.Triangles, 
		                   BufferUsageHint usage = BufferUsageHint.StaticDraw)
		{
			Debug.Assert(shader != null);
			Debug.Assert(attrs != null);
			AttributeLists = attrs;
			usageHint = usage;
			primitive = prim;
			NumberOfVerts = GetVertCount(attrs);
			vao = GL.GenVertexArray();
			GL.BindVertexArray(vao);
			buffer = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
			AddAttributesToBuffer(attrs);
			SetupVertexPointers(shader, attrs);
			// Unbinding the buffer *does not* alter the state of the vertex array object.
			// The association between buffer and vao is made on the GL.VertexAttribPointer() call.
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
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
			if (disposing) {
				// Clean up managed resources
				// This bit is only here in the unlikely event we need to do stuff
				// in the finalizer or override this in a child class or something, I guess.
				// But resource allocation is Important and Hard so I'm cleaving to the
				// recommended idiom in this.
			} else {
				// Clean up unmanaged resources
				// BUGGO: On the other hand, these calls crash the program whenever they happen, so.
				//GL.DeleteVertexArray(vao);
				//GL.DeleteBuffer(buffer);
			}
		}

		public void Dispose()
		{
			if (!disposed) {
				Dispose(true);
				// Don't run the finalizer, since it's a waste of time.
				GC.SuppressFinalize(this);
			}
		}

		int  GetVertCount(VertexAttributeArray[] attrs)
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

		void AddAttributesToBuffer(VertexAttributeArray[] attrs)
		{
			Debug.Assert(attrs != null);
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
			Debug.Assert(shader != null);
			Debug.Assert(attrs != null);
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
			GL.DrawArrays(primitive, 0, NumberOfVerts);
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

		public static readonly Color DEFAULT_COLOR = Color.White;
		public const double DEFAULT_STROKE_WIDTH = 2.0;
	}

	////////////////////////////////////////////////////////////////////////////////////////////
	/// Ported Drake code below here, might require fiddling, clarification, commentating.
	////////////////////////////////////////////////////////////////////////////////////////////

	/// <summary>
	/// A vertex to be part of a line art graphics model.
	/// Note that these objects are used for identity, not merely as
	/// value objects.
	/// </summary>
	public struct LineArtVertex
	{
		public Vector2d Pos;
		public Color Color;
		public double StrokeHalfWidth;

		public double X { 
			get {
				return Pos.X;
			}
		}

		public double Y { 
			get {
				return Pos.Y;
			}
		}

		public double StrokeWidth { 
			get {
				return StrokeHalfWidth * 2;
			}
		}
		// The nullable color here is a little hacky 'cause structs can't be const, or else their
		// initializer might do _anything_ at runtime and they won't actually be constant as in
		// 'fixed at compile time'.  Inconvenient, but true.
		public LineArtVertex(Vector2d pos, Color? color = null,
		                     double strokeWidth = Graphics.DEFAULT_STROKE_WIDTH / 2.0)
		{
			Pos = pos;
			Color = color ?? Graphics.DEFAULT_COLOR;
			StrokeHalfWidth = strokeWidth / 2.0;
		}

		public static LineArtVertex Lerp(LineArtVertex v0, LineArtVertex v1, double alpha, Vector2d? pos = null)
		{
			return new LineArtVertex(
				pos ?? Vector2d.Lerp(v0.Pos, v1.Pos, alpha),
				color: SMath.Lerp(v0.Color, v1.Color, alpha),
				strokeWidth: SMath.Lerp(v0.StrokeWidth, v1.StrokeWidth, alpha)
			);
		}
	}

	/// <summary>
	/// Abstract base class for segments that can be part of `Path`s.
	///
	/// Slots:
	/// `v0`, `v1` - starting and ending vertexes
	/// `before`, `after` - adjoining segments in the path
	/// `closing` - `True` iff this segment is the last segment in a closed path
	/// </summary>
	public abstract class PathSegment
	{
		public LineArtVertex V0;
		public LineArtVertex V1;
		public PathSegment Before;
		public PathSegment After;
		public bool Closing;

		public Vector2d Pos0 {
			get {
				return V0.Pos;
			}
		}

		public Vector2d Pos1 {
			get {
				return V1.Pos;
			}
		}

		public PathSegment(LineArtVertex v0, LineArtVertex v1)
		{
			V0 = v0;
			V1 = v1;
			// These are normally set by Path below
			Before = null;
			After = null;
			Closing = false;
		}
		// Implement the following methods in subclasses
		/// <summary>
		/// Call an appropriate method on `tessellator` for
		/// handling this segment using double-dispatch
		/// </summary>
		/// <param name="t">T.</param>
		public virtual void TesselateWith(Tesselator t)
		{
			Log.Assert(false);
		}

		/// <summary>
		/// Return the RawJoin for entering this segment at V0.
		/// </summary>
		/// <returns>The join in.</returns>
		public virtual RawJoin RawJoinIn()
		{
			Log.Assert(false);
			return new RawJoin();
		}

		/// <summary>
		/// Return the RawJoin for leaving this segment at V1.
		/// </summary>
		/// <returns>The join out.</returns>
		public virtual RawJoin RawJoinOut()
		{
			Log.Assert(false);
			return new RawJoin();
		}
	}

	/// <summary>
	/// The geometry of how a path segment should join at one end.
	///
	/// Each side vector is an offset from the segment endpoint, with
	/// `sideR` being on the 'right'.  The along vectors must be unit
	/// vectors and should point in the general direction of the path
	/// segment (thus, into the segment from the beginning, or out of it
	/// from the end).  A (side, along) pair defines a line.
	/// </summary>
	public struct RawJoin
	{
		public Vector2d SideR;
		public Vector2d SideL;
		public Vector2d AlongR;
		public Vector2d AlongL;

		public RawJoin(Vector2d sideR, Vector2d alongR, Vector2d sideL, Vector2d alongL)
		{
			SideR = sideR;
			AlongR = alongR;
			SideL = sideL;
			AlongL = alongL;
		}
	}

	/// <summary>
	/// The path segment for a straight line
	/// </summary>
	public class LineSegment : PathSegment
	{
		public bool Cap = true;

		public LineSegment(LineArtVertex v0, LineArtVertex v1) : base(v0, v1)
		{
		}

		RawJoin RawJoinAt(LineArtVertex v)
		{
			var along = (V1.Pos - V0.Pos).Normalized();
			var side = along.PerpendicularRight;
			// OPT: vector math
			return new RawJoin(side * v.StrokeWidth, along, side * -v.StrokeWidth, along);
		}

		public override RawJoin RawJoinIn()
		{
			return RawJoinAt(V0);
		}

		public override RawJoin RawJoinOut()
		{
			return RawJoinAt(V1);
		}

		public override void TesselateWith(Tesselator t)
		{
			t.TesselateLine(this);
		}
	}

	/// <summary>
	/// A path segment for a spiraling arc.
	///
	/// Slots:
	/// `center` - the center of the spiral
	/// `clockwise` - True iff the angle from v0 to v1 is clockwise
	/// `requestedSegments` - null or an integer
	/// </summary>
	public class ArcSegment : PathSegment
	{
		Vector2d Center;
		bool Clockwise;
		int? RequestedSegments;
		const double RADIANS_PER_PIECE = SMath.TAU / 16.0;

		public ArcSegment(LineArtVertex v0, LineArtVertex v1, Vector2d center,
		                  bool? clockwise = null, int? requestedSegments = null)
			: base(v0, v1)
		{
			Center = center;
			Clockwise = clockwise ?? SMath.CrossZ(V0.Pos - Center, V1.Pos - Center) < 0;
			RequestedSegments = requestedSegments;
		}

		public RawJoin RawJoinAt(LineArtVertex v)
		{
			var side = (v.Pos - Center).Normalized();
			if (Clockwise) {
				side = -side;
			}

			var along = -side.PerpendicularRight;
			return new RawJoin(side * v.StrokeHalfWidth, along,
			                   side * -v.StrokeHalfWidth, along);
		}

		public override RawJoin RawJoinIn()
		{
			return RawJoinAt(V0);
		}

		public override RawJoin RawJoinOut()
		{
			return RawJoinAt(V1);
		}

		public override void TesselateWith(Tesselator t)
		{
			t.TesselateArc(this);
		}

		/// <summary>
		/// Yield zero or more (vertex, sideR, sideL) triples.
		/// Note this is an iterator!
		///
		/// Each triple corresponds to an intermediary virtual vertex on
		/// the arc, spaced suitably for approximating the arc as lines,
		/// with sideR and sideL being vectors as in a RawJoin.  Each
		/// vertex holds full vertex data.
		/// </summary>
		/// <returns>The interior points.</returns>
		public IEnumerable<Tuple<LineArtVertex, Vector2d, Vector2d>>
			GenerateInteriorPoints()
		{
			// Everything is in radians in this function because converting to degrees and back would
			// be rather silly.

			var rel0 = V0.Pos - Center;
			var rel1 = V1.Pos - Center;
			var radius0 = rel0.Length;
			var radius1 = rel1.Length;
			var angle0 = Math.Atan2(V0.Pos.Y - Center.Y, V0.Pos.X - Center.X);
			var angle1 = Math.Atan2(V1.Pos.Y - Center.Y, V1.Pos.X - Center.X);
			if (Clockwise && angle0 < angle1) {
				angle1 -= SMath.TAU;
			} else if (!Clockwise && angle0 >= angle1) {
				angle1 += SMath.TAU;
			}
			var nPieces = RequestedSegments ?? Math.Max(3, (int)Math.Ceiling(Math.Abs(angle1 - angle0) / RADIANS_PER_PIECE));
			var deltaAlpha = 1.0 / nPieces;
			var deltaRadius = (radius1 - radius0) * deltaAlpha;
			var deltaAngle = (angle1 - angle0) * deltaAlpha;
			var deltaStrokeHalf = (V1.StrokeHalfWidth - V0.StrokeHalfWidth) * deltaAlpha;
			if (Clockwise) {
				// This corresponds to the negation of curStrokeHalf below
				deltaStrokeHalf = -deltaStrokeHalf;
			}

			var sinDA = Math.Sin(deltaAngle);
			var cosDA = Math.Cos(deltaAngle);
			var rel0Unit = rel0.Normalized();

			var unitX = rel0Unit.X;
			var unitY = rel0Unit.Y;
			var curRadius = radius0;
			var curStrokeHalf = V0.StrokeHalfWidth;
			if (Clockwise) {
				curStrokeHalf = -curStrokeHalf;
			}
			// We only need to generate nPieces-1 points because the start and end points are both excluded.
			// This might be more numerically stable than the tan/cos method and still doesn't take a sin/cos
			// every iteration.  We shouldn't be doing this very often anyway.
			for (int i = 0; i < nPieces; i++) {
				curStrokeHalf += deltaStrokeHalf;
				curRadius += deltaRadius;
				unitX = unitX * cosDA - unitY - sinDA;
				unitY = unitX * sinDA - unitY - cosDA;
				var interiorPos = new Vector2d(Center.X + unitX * curRadius, Center.Y + unitY * curRadius);
				var interior = LineArtVertex.Lerp(V0, V1, i * deltaAlpha, pos: interiorPos);
				var sideR = new Vector2d(unitX * curStrokeHalf, unitY * curStrokeHalf);
				yield return new Tuple<LineArtVertex, Vector2d, Vector2d>(interior, sideR, -sideR);
			}
			yield break;

		}
	}

	public class Tesselator
	{
		public void TesselateLine(LineSegment l)
		{

		}

		public void TesselateArc(ArcSegment a)
		{

		}
	}
}

