using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;

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

