using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	////////////////////////////////////////////////////////////////////////////////////////////
	/// Ported Drake code below here, might require fiddling, clarification, commentating.
	////////////////////////////////////////////////////////////////////////////////////////////

	/// <summary>
	/// A vertex to be part of a line art graphics model.
	/// Note that these objects are used for identity, not merely as
	/// value objects.
	/// </summary>
	public struct LineArtVertex : IEquatable<LineArtVertex>
	{

		public static readonly Color4 DEFAULT_COLOR = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
		public const double DEFAULT_STROKE_WIDTH = 0.25;
		public const double EPSILON = 0.0001;
		public Vector2d Pos;
		public Color4 Color;
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
		public LineArtVertex(Vector2d pos, Color4? color = null,
		                     double strokeWidth = DEFAULT_STROKE_WIDTH)
		{
			Pos = pos;
			Color = color ?? DEFAULT_COLOR;
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

		public bool Equals(LineArtVertex other)
		{
			return Pos == other.Pos && Color == other.Color && Math.Abs(StrokeHalfWidth - other.StrokeHalfWidth) < EPSILON;
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
		public bool Cap = false;

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
		public abstract void TesselateWith(Tesselator t);

		/// <summary>
		/// Return the RawJoin for entering this segment at V0.
		/// </summary>
		/// <returns>The join in.</returns>
		public abstract RawJoin RawJoinIn();

		/// <summary>
		/// Return the RawJoin for leaving this segment at V1.
		/// </summary>
		/// <returns>The join out.</returns>
		public abstract RawJoin RawJoinOut();
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

		public LineSegment(LineArtVertex v0, LineArtVertex v1) : base(v0, v1)
		{
			Cap = true;
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
		int RequestedSegments;
		const int DEFAULT_SEGMENTS = 16;
		const double RADIANS_PER_PIECE = SMath.TAU / DEFAULT_SEGMENTS;

		public ArcSegment(LineArtVertex v0, LineArtVertex v1, Vector2d center,
		                  bool? clockwise = null, int? requestedSegments = null)
			: base(v0, v1)
		{
			Center = center;
			Clockwise = clockwise ?? SMath.CrossZ(V0.Pos - Center, V1.Pos - Center) < 0;
			// XXX: Might be better to come up with something based on RADIANS_PER_PIECE
			// and the angle span of the arc.
			RequestedSegments = requestedSegments ?? DEFAULT_SEGMENTS;
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

			//var offset0 = V0.Pos - Center;
			//var offset1 = V1.Pos - Center;
			var angle0 = Math.Atan2(V0.Pos.Y - Center.Y, V0.Pos.X - Center.X);
			var angle1 = Math.Atan2(V1.Pos.Y - Center.Y, V1.Pos.X - Center.X);
			if (Clockwise && (angle0 <= angle1)) {
				angle1 -= SMath.TAU;
			} else if ((!Clockwise) && (angle0 >= angle1)) {
				angle1 += SMath.TAU;
			}
			var angleStep = (angle1 - angle0) / RequestedSegments;

			var radius0 = (V0.Pos - Center).Length;
			var radius1 = (V1.Pos - Center).Length;
			//Log.Message("Radius 0 {0}, Radius 1 {1}", radius0, radius1);
			var radiusStep = (radius1 - radius0) / RequestedSegments;

			var strokeHalfWidth0 = V0.StrokeHalfWidth;
			var strokeHalfWidth1 = V1.StrokeHalfWidth;
			var strokeHalfWidthStep = strokeHalfWidth1 - strokeHalfWidth0;

			// This is alpha not in terms of color blending but rather in terms of
			// how far we are through a lerp.
			var alphaStep = 1.0 / RequestedSegments;

			var currentAlpha = 0.0;
			var currentAngle = angle0;
			var currentRadius = radius0;
			var currentX = Math.Cos(currentAngle) * radius0;
			var currentY = Math.Sin(currentAngle) * radius0;
			var currentStrokeHalfWidth = strokeHalfWidth0;
			for (int i = 1; i < RequestedSegments; i++) {
				currentAlpha += alphaStep;
				currentAngle += angleStep;
				currentRadius += radiusStep;

				currentX = Math.Cos(currentAngle) * currentRadius;
				currentY = Math.Sin(currentAngle) * currentRadius;
				currentStrokeHalfWidth += strokeHalfWidthStep;

				//Console.WriteLine("Generating point at x {0}, y {1}, radius {2}, angle {3}", currentX, currentY, currentRadius, currentAngle);

				var currentOffset = new Vector2d(currentX, currentY);
				var currentPosition = currentOffset + Center;
				var currentVertex = LineArtVertex.Lerp(V0, V1, currentAlpha, pos: currentPosition);
				var currentNormal = currentOffset.Normalized();
				var inner = currentNormal * currentStrokeHalfWidth;
				var outer = -inner;
				//Log.Message("Current angle: {0}, X: {1}, Y: {2}, center: {3}, alpha: {4}, angle0: {5}, angle1: {6}", 
				//	currentAngle, currentX, currentY, Center, currentAlpha, angle0, angle1);
				yield return new Tuple<LineArtVertex, Vector2d, Vector2d>(currentVertex, outer, inner);
			}

			/*
			var rel0 = V0.Pos - Center;
			var rel1 = V1.Pos - Center;
			var radius0 = rel0.Length;
			var radius1 = rel1.Length;
			var angle0 = Math.Atan2(V0.Pos.Y - Center.Y, V0.Pos.X - Center.X);
			var angle1 = Math.Atan2(V1.Pos.Y - Center.Y, V1.Pos.X - Center.X);
			if (Clockwise && angle0 <= angle1) {
				angle1 -= SMath.TAU;
			} else if ((!Clockwise) && (angle0 >= angle1)) {
				angle1 += SMath.TAU;
			}
			var nPieces = RequestedSegments ?? Math.Max(3, (int)Math.Ceiling(Math.Abs(angle1 - angle0) / RADIANS_PER_PIECE));
			var deltaAlpha = 1.0 / nPieces;
			var deltaRadius = (radius1 - radius0) * deltaAlpha;
			var deltaAngle = (angle1 - angle0) * deltaAlpha;
			var deltaStrokeHalf = (V1.StrokeHalfWidth - V0.StrokeHalfWidth) * deltaAlpha;

			var sinDA = Math.Sin(deltaAngle);
			var cosDA = Math.Cos(deltaAngle);
			var rel0Unit = rel0.Normalized();

			var unitX = rel0Unit.X;
			var unitY = rel0Unit.Y;
			var curRadius = radius0;
			var curStrokeHalf = V0.StrokeHalfWidth;
			if (Clockwise) {
				curStrokeHalf = -curStrokeHalf;
				deltaStrokeHalf = -deltaStrokeHalf;
			}
			// We only need to generate nPieces-1 points because the start and end points are both excluded.
			// This might be more numerically stable than the tan/cos method and still doesn't take a sin/cos
			// every iteration.  We shouldn't be doing this very often anyway.
			for (int i = 1; i < nPieces; i++) {
				curStrokeHalf += deltaStrokeHalf;
				curRadius += deltaRadius;
				unitX = unitX * cosDA - unitY * sinDA;
				unitY = unitX * sinDA - unitY * cosDA;
				var interiorPos = new Vector2d(Center.X + unitX * curRadius, Center.Y + unitY * curRadius);
				var interior = LineArtVertex.Lerp(V0, V1, i * deltaAlpha, pos: interiorPos);
				var sideR = new Vector2d(unitX * curStrokeHalf, unitY * curStrokeHalf);
				yield return new Tuple<LineArtVertex, Vector2d, Vector2d>(interior, sideR, -sideR);
			}
			yield break;
			*/
		}
	}

	/// <summary>
	/// A single 'stroke' of the pen.
	/// 
	/// Disjoint paths are not currently supported.
	/// 
	/// Segments: A list of path segments
	/// Closed: True iff the last segment adjoins back to the first.
	/// </summary>
	public class Path
	{
		public List<PathSegment> Segments;
		public bool Closed;

		public Path()
		{
			Segments = new List<PathSegment>();
			Closed = false;
		}

		/// <summary>
		/// Append a segment to the path.
		/// 
		/// 'segment' must begin at the same vertex the last segment ended at.
		/// </summary>
		/// <param name="">.</param>
		public void AddSegment(PathSegment segment)
		{
			// BUGGO: Fix this.
			// LineArtVertex needs to be comparable.
			//Log.Assert(Segments.Count != 0 || segment.V0 == Segments[Segments.Count - 1].V1);
			Log.Assert(!Closed);

			if (Segments.Count != 0) {
				var lastSegment = Segments[Segments.Count - 1];
				segment.Before = lastSegment;
				lastSegment.After = segment;
			}
			Segments.Add(segment);
		}

		/// <summary>
		/// Close the path.
		/// 
		/// If closingSegment is specified, use it; it must begin at the
		/// ending vertex of the current last segment of the path and 
		/// end at the beginning of the path.  It closingSegment is null,
		/// generate an appropriate segment to use.
		/// </summary>
		/// <param name="closingSegment">The segment to close the path with; generated automatically if null.</param>
		public void Close(PathSegment closingSegment = null)
		{
			if (closingSegment == null) {
				closingSegment = new LineSegment(Segments[Segments.Count - 1].V1, Segments[0].V0);
			}

			AddSegment(closingSegment);
			// This works (and must continue to work) even if the closing segment is
			// also the opening segment.
			closingSegment.After = Segments[0];
			Segments[0].Before = closingSegment;
			closingSegment.Closing = true;
			Closed = true;
		}

		public bool Empty { 
			get {
				return Segments.Count == 0;
			}
		}
	}

	/// <summary>
	/// A class representing a filled, solid-color polygon.
	/// For now, it MUST be convex!
	/// </summary>
	public class FilledPolygon
	{
		List<LineArtVertex> Vertexes;

		public FilledPolygon()
		{
			Vertexes = new List<LineArtVertex>();
		}

		public FilledPolygon(IEnumerable<Vector2d> points, Color4 color) : this()
		{
			foreach (var p in points) {
				AddVertex(p, color);
			}
		}

		public FilledPolygon(IEnumerable<LineArtVertex> verts) : this()
		{
			foreach (var v in verts) {
				AddVertex(v);
			}
		}

		public void AddVertex(LineArtVertex v)
		{
			Vertexes.Add(v);
		}

		public void AddVertex(Vector2d pos, Color4 color)
		{
			Vertexes.Add(new LineArtVertex(pos, color: color));
		}

		public VertexModel ToVertexModel()
		{
			var output = new VertexModel();
			Tesselate(output);
			return output;
		}

		public void Tesselate(VertexModel output)
		{
			var firstIdx = output.AddVertexes(Vertexes);
			var lastIdx = firstIdx + Vertexes.Count;
			// We triangulate a convex poly just by drawing lines from the first point to
			// all the others...
			for (uint i = firstIdx + 2; i < lastIdx; i++) {
				var tri = new uint[] {
					firstIdx, i - 1, i
				};
				output.AddIndices(tri);
			}
		}
	}

	/// <summary>
	/// A model with vertex data suitable for uploading to OpenGL
	/// (which means creating a VertexArray).
	/// </summary>
	// XXX: Currently this doesn't handle textured anything...
	public class VertexModel
	{
		const int POSITION_DIMENSIONS = 2;
		const int COLOR_DIMENSIONS = 4;
		List<Vector2> positions;
		List<Color4> colors;
		List<uint> indices;
		uint freeIndex;

		public VertexModel()
		{
			//positions = new List<float>();
			//colors = new List<float>();

			positions = new List<Vector2>();
			colors = new List<Color4>();
			indices = new List<uint>();
			freeIndex = 0;
		}

		/// <summary>
		/// Append Vertexes to vertex data.
		/// </summary>
		/// <returns>Index of the first newly-appended index.</returns>
		/// <param name="Vertexes">Vertexes.</param>
		public uint AddVertexes(IEnumerable<LineArtVertex> Vertexes)
		{
			uint vertexCount = 0;
			foreach (var vertex in Vertexes) {
				// Note that this is where we go from double-precision coordinates to 
				// single-precision coordinates for drawing.
				var pos = new Vector2((float)vertex.Pos.X, (float)vertex.Pos.Y);
				positions.Add(pos);
				colors.Add(vertex.Color);
				vertexCount += 1;
			}
			var firstIndex = freeIndex;
			freeIndex += vertexCount;
			return firstIndex;
		}

		/// <summary>
		/// Append 'ind' to the list of indices to draw.
		/// 
		/// Normally it will have a length that is a multiple of 3, for drawing triangles.
		/// </summary>
		/// <param name="ind">Collection of indices.</param>
		public void AddIndices(IEnumerable<uint> ind)
		{
			indices.AddRange(ind);
		}

		/// <summary>
		/// Returns a new vertex array containing the data in the VertexModel.
		/// Makes some assumptions about what you're going to be drawing.
		/// </summary>
		/// <returns>The vertex array.</returns>
		public VertexArray ToVertexArray(Shader s)
		{
			var verts = new VertexList(VertexLayout.ColorVertex);
			for (int i = 0; i < positions.Count; i++) {
				verts.AddColorVertex(positions[i], colors[i]);
			}
			var vertArray = new VertexArray(s, verts, indices,
				                prim: PrimitiveType.Triangles, usage: BufferUsageHint.StaticDraw);

			return vertArray;

		}
	}

	/// <summary>
	/// Base class for all tesselators, which are objects that take a Path
	/// and turn it into actual verts in a VertexModel
	/// </summary>
	public abstract class Tesselator
	{
		protected readonly Color4 BACKGROUND_COLOR = new Color4(0.0f, 0.0f, 0.0f, 0.0f);
		protected VertexModel Output;

		public Tesselator(VertexModel output)
		{
			this.Output = output;
		}

		protected IEnumerable<uint> AddVertexes(IList<LineArtVertex> verts)
		{
			uint start = Output.AddVertexes(verts);
			return Util.UnsignedRange(start, (uint)(start + verts.Count));
		}

		/// <summary>
		/// Create a LineArtVertex with the default background color.
		/// </summary>
		/// <param name="pos">Position.</param>
		public LineArtVertex Background(Vector2d pos)
		{
			return new LineArtVertex(pos, BACKGROUND_COLOR);
		}

		protected void AddQuad(uint a, uint b, uint c, uint d)
		{
			var quadIndices = new uint[] { a, b, c, a, c, d };
			Output.AddIndices(quadIndices);
		}

		public abstract void TesselatePath(Path path);

		public abstract void TesselateLine(LineSegment line);

		public abstract void TesselateArc(ArcSegment arc);
	}

	/// <summary>
	/// Tessellate lines and arcs to make solid lines.
	/// 
	/// Each base vertex encountered along a path is converted into a
	/// string of three subvertexes: the central one plus right and left
	/// edges.  Subvertexes at the same position in each string form the
	/// borders of quadrilaterals, which are each rendered using two
	/// triangles.
	///
	/// Multiple paths may be tessellated using a single tessellator so long as
	/// `beginPath`() and `endPath`() are called properly.  If you use the main
	/// entrypoint `tessellatePath`(), this is done automatically.
	/// </summary>
	public class LineTesselator : Tesselator
	{
		protected List<uint> firstIndices;
		protected List<uint> lastIndices;

		public LineTesselator(VertexModel output) : base(output)
		{
			firstIndices = null;
			lastIndices = null;
		}

		public void BeginPath()
		{
			firstIndices = null;
			lastIndices = null;
		}

		public void EndPath()
		{
			firstIndices = null;
			lastIndices = null;
		}

		void BeginRoad(IList<LineArtVertex> verts)
		{
			var indices = AddVertexes(verts);
			firstIndices = new List<uint>(indices);
			lastIndices = new List<uint>(indices);
		}

		void AdvanceTo(IList<LineArtVertex> verts)
		{
			var indices = new List<uint>(AddVertexes(verts));
			//Console.WriteLine("Verts length: {0}, indices length: {1}", verts.Count, indices.Count);

			AddQuad(lastIndices[0], lastIndices[1], indices[1], indices[0]);

			lastIndices = indices;
		}

		void CloseRoad()
		{
			AddQuad(lastIndices[0], lastIndices[1], firstIndices[1], firstIndices[0]);
			firstIndices = null;
			lastIndices = null;
		}

		void AdvanceToSegmentIn(PathSegment seg)
		{
			if (seg.Before == null) {
				var rjIn = seg.RawJoinIn();
				var nextVerts = new LineArtVertex[] {
					new LineArtVertex(seg.Pos0 + rjIn.SideR, color: seg.V0.Color),
					new LineArtVertex(seg.Pos0 + rjIn.SideL, color: seg.V0.Color),
				};
				if (seg.Cap) {
					var cap = SquarishCap(rjIn, -1.0);
					var cap0 = cap.Item1;
					var cap1 = cap.Item2;
					var beginVerts = new LineArtVertex[] {
						new LineArtVertex(seg.Pos0 + cap0, color: seg.V0.Color),
						new LineArtVertex(seg.Pos0 + cap1, color: seg.V0.Color),
					};
					BeginRoad(beginVerts);
					AdvanceTo(nextVerts);
				} else {
					// No cap
					BeginRoad(nextVerts);
				}
			} else if (seg.Before.Closing || (lastIndices == null)) {
				// The RHS of the disjunction is in case we start tessellating in the middle of a
				// path somehow.  That feels like it might be useful later.
				var jn = MiterishJoin(seg.Before.RawJoinOut(), seg.RawJoinIn());
				var sideR = jn.Item1;
				var sideL = jn.Item2;
				var verts = new LineArtVertex[] {
					new LineArtVertex(seg.Pos0 + sideR, color: seg.V0.Color),
					new LineArtVertex(seg.Pos0 + sideL, color: seg.V0.Color),
				};
				BeginRoad(verts);
			}
		}

		void AdvanceToSegmentOut(PathSegment seg)
		{
			if (seg.After == null) {
				var rjOut = seg.RawJoinOut();
				var nextVerts = new LineArtVertex[] {
					new LineArtVertex(seg.Pos1 + rjOut.SideR, color: seg.V1.Color),
					new LineArtVertex(seg.Pos1 + rjOut.SideL, color: seg.V1.Color),
				};
				if (seg.Cap) {
					var cap = SquarishCap(rjOut, +1.0);
					var cap0 = cap.Item1;
					var cap1 = cap.Item2;
					var endVerts = new LineArtVertex[] {
						new LineArtVertex(seg.Pos1 + cap0, color: seg.V1.Color),
						new LineArtVertex(seg.Pos1 + cap1, color: seg.V1.Color)
					};
					AdvanceTo(nextVerts);
					AdvanceTo(endVerts);
				} else {
					AdvanceTo(nextVerts);
				}
			} else if (seg.Closing) {
				CloseRoad();
			} else {
				var jn = MiterishJoin(seg.RawJoinOut(), seg.After.RawJoinIn());
				var sideR = jn.Item1;
				var sideL = jn.Item2;
				var nextVerts = new LineArtVertex[] {
					new LineArtVertex(seg.Pos1 + sideR, color: seg.V1.Color),
					new LineArtVertex(seg.Pos1 + sideL, color: seg.V1.Color)
				};
				AdvanceTo(nextVerts);
			}
		}

		public override void TesselateLine(LineSegment line)
		{
			AdvanceToSegmentIn(line);
			AdvanceToSegmentOut(line);
		}

		public override void TesselateArc(ArcSegment arc)
		{
			AdvanceToSegmentIn(arc);
			foreach (var pt in arc.GenerateInteriorPoints()) {
				var interior = pt.Item1;
				var sideR = pt.Item2;
				var sideL = pt.Item3;

				var verts = new LineArtVertex[] {
					new LineArtVertex(interior.Pos + sideR, color: interior.Color),
					new LineArtVertex(interior.Pos + sideL, color: interior.Color)
				};

				AdvanceTo(verts);
			}
			AdvanceToSegmentOut(arc);
		}

		public override void TesselatePath(Path path)
		{
			if (path.Empty) {
				return;
			}
			BeginPath();
			foreach (var seg in path.Segments) {
				seg.TesselateWith(this);
			}
			EndPath();
		}

		/// <summary>
		/// Given a RawJoin, compute the corners of a square cap.
		/// </summary>
		/// <returns>The cap.</returns>
		/// <param name="rj">The RawJoin</param>
		/// <param name="multiplier">Usually -1.0 for backward or +1.0 for forward, 
		/// depending on the segment being capped.</param>
		static Tuple<Vector2d, Vector2d> SquarishCap(RawJoin rj, double multiplier)
		{
			var extent = multiplier * (0.5 * (rj.SideR - rj.SideL).Length);
			return new Tuple<Vector2d, Vector2d>(rj.SideR + rj.AlongR * extent, rj.SideL + rj.AlongL * extent);
		}

		/// <summary>
		/// Given two adjoining (side, along) pairs, computer a miter-ish point for them.
		/// 
		// These are named A and B because they're on the same side but come from two different segments.
		// Solve for position = (sideA + alongA * parameterA) = (sideB + alongB * parameterB)
		// (ie, just a line intersection).
		/// </summary>
		/// <returns>The intersect.</returns>
		/// <param name="sideA">Side a.</param>
		/// <param name="alongA">Along a.</param>
		/// <param name="sideB">Side b.</param>
		/// <param name="alongB">Along b.</param>
		// OPT: This may not be the most efficient way.
		static Vector2d MiterishIntersect(Vector2d sideA, Vector2d alongA, Vector2d sideB, Vector2d alongB)
		{
			// Transform to origin at sideA, +x is alongA.
			var tlSideB = sideB - sideA;
			var relSideB = new Vector2d(Vector2d.Dot(tlSideB, alongA), Vector2d.Dot(tlSideB, alongA.PerpendicularLeft));
			var rotAlongB = new Vector2d(Vector2d.Dot(alongB, alongA), Vector2d.Dot(alongB, alongA.PerpendicularLeft));
			if (Math.Abs(rotAlongB.Y) < Math.Abs(rotAlongB.X) && Math.Abs(rotAlongB.Y / rotAlongB.X) < 0.001) {
				// Very closely angled lines.  Give up and approximate for numerical stability.
				return Vector2d.Lerp(sideA, sideB, 0.5);
			}

			// B's transformed line equation in terms of A
			var xSlope = rotAlongB.X / rotAlongB.Y;
			var xIntercept = relSideB.X - relSideB.Y * xSlope;
			// Transform (xIntercept, 0) back into world coordinates.
			return sideA + alongA * xIntercept;
		}

		/// <summary>
		/// Given two adjoining RawJoin's, computer the right and left sides of a miter-ish join.
		/// </summary>
		/// <returns>The join.</returns>
		static Tuple<Vector2d, Vector2d> MiterishJoin(RawJoin rjIn, RawJoin rjOut)
		{
			var v1 = MiterishIntersect(rjIn.SideR, rjIn.AlongR, rjOut.SideR, rjOut.AlongR);
			var v2 = MiterishIntersect(rjIn.SideL, rjIn.AlongL, rjOut.SideL, rjOut.AlongL);

			return new Tuple<Vector2d, Vector2d>(v1, v2);
		}
	}

	/// <summary>
	/// A convenient way to construct `VertexModel`s.
	///
	/// Create one of these, then call methods on it to add geometry.
	/// This is somewhat similar to using a Cairo context.  When done,
	/// call `finish`() to obtain the finished model.  You should not
	/// call `finish`() twice.
	/// </summary>
	public class ModelBuilder
	{
		VertexModel model;
		Tesselator tesselator;

		public ModelBuilder()
		{
			model = new VertexModel();
			tesselator = new LineTesselator(model);
		}

		public VertexModel Finish()
		{
			return model;
		}

		public void SubmitPath(Path path)
		{
			tesselator.TesselatePath(path);
		}

		public void SubmitClosedPath(IList<PathSegment> segments)
		{
			var path = new Path();
			for (int i = 0; i < segments.Count - 1; i++) {
				path.AddSegment(segments[i]);
			}
			path.Close(segments[segments.Count - 1]);
			SubmitPath(path);
		}
		// BUGGO: Make sure this works correctly.
		public void SubmitClosedPath(PathSegment segment)
		{
			var path = new Path();
			path.Close(segment);
			SubmitPath(path);
		}

		public void SubmitOpenPath(IList<PathSegment> segments)
		{
			var path = new Path();
			foreach (var segment in segments) {
				path.AddSegment(segment);
			}
			SubmitPath(path);
		}

		public void SubmitOpenPath(PathSegment segment)
		{
			var path = new Path();
			path.AddSegment(segment);
			SubmitPath(path);
		}
		// TODO: These don't have a way to specify line thickness...
		public void Circle(double x, double y, double radius, Color4 color, int? numSegments = null)
		{
			var vertex = new LineArtVertex(new Vector2d(x + radius, y), color);
			var verts = new PathSegment[] {
				new ArcSegment(vertex, vertex, new Vector2d(x, y), true, numSegments)
			};
			SubmitClosedPath(verts);
		}

		public void CircleFilled(double x, double y, double radius, Color4 color, int? numSegments = null)
		{
			var segments = numSegments ?? 16;
			var radiansPerSegment = SMath.TAU / segments;
			var angle = 0.0;
			var verts = new List<LineArtVertex>();
			for (int i = 0; i < segments; i++) {
				var pos = new Vector2d(Math.Cos(angle) * radius + x, Math.Sin(angle) * radius + y);
				angle += radiansPerSegment;
				verts.Add(new LineArtVertex(pos, color));
			}
			PolygonFilled(verts);
		}

		public void CircleOutlined(double x, double y, double radius, Color4 color, Color4 outlineColor, int? numSegments = null)
		{
			CircleFilled(x, y, radius, color, numSegments: numSegments);
			Circle(x, y, radius, outlineColor, numSegments: numSegments);

		}

		public void Arc(double cx, double cy, double radius, double sweep, Color4 color, double startAngle = 0.0,
		                int? numSegments = null)
		{
			var pos0 = SMath.Rotate(Vector2d.UnitX, startAngle) * radius;
			var pos1 = SMath.Rotate(Vector2d.UnitX, startAngle + sweep) * radius;
			//Log.Message("Start angle {0}, sweep {1}, radius {2}, pos0 {3}, pos1 {4}, length1 {5}, length2 {6}",
			//                  startAngle, sweep, radius, pos0, pos1, pos0.Length, pos1.Length);
			var v0 = new LineArtVertex(pos0, color: color);
			var v1 = new LineArtVertex(pos1, color: color);
			SubmitOpenPath(new ArcSegment(v1, v0, new Vector2d(cx, cy), true, numSegments));
		}

		public void Line(double x0, double y0, double x1, double y1, Color4 color)
		{
			var v0 = new LineArtVertex(new Vector2d(x0, y0), color: color);
			var v1 = new LineArtVertex(new Vector2d(x1, y1), color: color);
			SubmitOpenPath(new LineSegment(v0, v1));
		}

		// XXX: TODO: Add more error checking (or push more of it upstream),
		// clarify IList vs. IEnumerable
		public void Polygon(IList<LineArtVertex> verts)
		{
			var segments = new List<PathSegment>();
			for (int i = 0; i < verts.Count - 1; i++) {
				var segment = new LineSegment(verts[i], verts[i + 1]);
				segments.Add(segment);
			}
			// Close the loop
			segments.Add(new LineSegment(verts[verts.Count - 1], verts[0]));
			SubmitClosedPath(segments);
		}

		public void PolygonFilled(IEnumerable<LineArtVertex> verts)
		{
			var p = new FilledPolygon(verts);
			p.Tesselate(model);
		}

		// XXX: Needs outline width's
		public void PolygonOutlined(IList<LineArtVertex> verts, IList<LineArtVertex> outline)
		{
			PolygonFilled(verts);
			Polygon(outline);

		}

		public void PolygonUniform(IEnumerable<Vector2d> positions, Color4 color)
		{
			var verts = new List<LineArtVertex>();
			foreach (var pos in positions) {
				var v = new LineArtVertex(pos, color: color);
				verts.Add(v);
			}
			Polygon(verts);
		}

		public void PolygonUniformFilled(IEnumerable<Vector2d> positions, Color4 color)
		{
			var p = new FilledPolygon(positions, color);
			p.Tesselate(model);
		}

		public void PolygonUniformOutlined(IEnumerable<Vector2d> positions, Color4 color, Color4 outlinecolor)
		{
			PolygonUniformFilled(positions, color);
			PolygonUniform(positions, outlinecolor);
		}

		public void PolyLine(IList<LineArtVertex> verts)
		{
			var segments = new List<PathSegment>();
			for (int i = 0; i < verts.Count - 1; i++) {
				var segment = new LineSegment(verts[i], verts[i + 1]);
				segments.Add(segment);
			}
			SubmitOpenPath(segments);
		}

		public void PolyLineUniform(IEnumerable<Vector2d> positions, Color4 color)
		{
			var verts = new List<LineArtVertex>();
			foreach (var pos in positions) {
				var v = new LineArtVertex(pos, color: color);
				verts.Add(v);
			}
			PolyLine(verts);
		}

		public void RectCenter(double cx, double cy, double w, double h, Color4 color)
		{
			var halfW = w / 2;
			var halfH = h / 2;
			var positions = new Vector2d[] {
				new Vector2d(cx - halfW, cy - halfH),
				new Vector2d(cx - halfW, cy + halfH),
				new Vector2d(cx + halfW, cy + halfH),
				new Vector2d(cx + halfW, cy - halfH),
			};

			PolygonUniform(positions, color);
		}

		public void RectCorner(double x0, double y0, double w, double h, Color4 color)
		{
			var positions = new Vector2d[] {
				new Vector2d(x0, y0),
				new Vector2d(x0, y0 + h),
				new Vector2d(x0 + w, y0 + h),
				new Vector2d(x0 + w, y0),
			};
			PolygonUniform(positions, color);
		}

		public void RectCenterFilled(double cx, double cy, double w, double h, Color4 color)
		{
			var halfW = w / 2;
			var halfH = h / 2;
			var positions = new Vector2d[] {
				new Vector2d(cx - halfW, cy - halfH),
				new Vector2d(cx - halfW, cy + halfH),
				new Vector2d(cx + halfW, cy + halfH),
				new Vector2d(cx + halfW, cy - halfH),
			};
			PolygonUniformFilled(positions, color);
		}

		public void RectCenterOutlined(double cx, double cy, double w, double h, Color4 color, Color4 outlinecolor)
		{
			var halfW = w / 2;
			var halfH = h / 2;
			var positions = new Vector2d[] {
				new Vector2d(cx - halfW, cy - halfH),
				new Vector2d(cx - halfW, cy + halfH),
				new Vector2d(cx + halfW, cy + halfH),
				new Vector2d(cx + halfW, cy - halfH),
			};
			PolygonUniformOutlined(positions, color, outlinecolor);
		}

		public void RectCornerFilled(double x0, double y0, double w, double h, Color4 color)
		{
			var positions = new Vector2d[] {
				new Vector2d(x0, y0),
				new Vector2d(x0, y0 + h),
				new Vector2d(x0 + w, y0 + h),
				new Vector2d(x0 + w, y0),
			};
			PolygonUniformFilled(positions, color);
		}

		public void RectCornerOutlined(double x0, double y0, double w, double h, Color4 color, Color4 outlinecolor)
		{
			var positions = new Vector2d[] {
				new Vector2d(x0, y0),
				new Vector2d(x0, y0 + h),
				new Vector2d(x0 + w, y0 + h),
				new Vector2d(x0 + w, y0),
			};
			PolygonUniformOutlined(positions, color, outlinecolor);
		}
	}


	public static class TextDrawer
	{
		public static Texture RenderString(string s, Color4 color, int fontSize = 24)
		{
			var textColor = Util.FromColor4(color);
			var font = new Font(FontFamily.GenericMonospace, fontSize, FontStyle.Bold);
			var dummy = new Bitmap(1, 1);
			SizeF size;
			using (var graphics = System.Drawing.Graphics.FromImage(dummy)) {
				// We need this stupid dummy drawing context because System.Drawing.Graphics is sorta lame and assumes 
				// you never need to know how big a string is _before_ allocating memory for it.
				// The alternative is adding a dependency on Windows.Forms, so, forget that.
				size = graphics.MeasureString(s, font);
			}
			// BUGGO: This squishes the drawn text into a square shape no matter what shape it is!
			var pow2Width = (int)SMath.RoundUpToPowerOf2(size.Width);
			var pow2Height = (int)SMath.RoundUpToPowerOf2(size.Height);
			var bitmap = new Bitmap(pow2Width, pow2Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			using (var graphics = System.Drawing.Graphics.FromImage(bitmap)) {
				graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, pow2Width, pow2Height);
				graphics.DrawString(s, font, new SolidBrush(textColor), 0, 0);
				graphics.Flush();
			}
			return new Texture(bitmap);
		}
	}
}

