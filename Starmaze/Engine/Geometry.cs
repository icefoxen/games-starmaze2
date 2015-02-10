using System;
using OpenTK;
using Newtonsoft.Json;

namespace Starmaze.Engine
{
	/// <summary>
	/// A bounding box.
	/// </summary>
	public class BBox
	{
		public double X0;
		public double Y0;
		public double X1;
		public double Y1;

		public double Dx { 
			get { 
				return Right - Left;
			}
		}

		public double Dy {
			get {
				return Top - Bottom;
			}
		}

		/// <summary>
		/// Gets the bottom left corner.
		/// </summary>
		/// <value>The bottom left corner.</value>
		[JsonConverter(typeof(OTKVector2dConverter))]
		public Vector2d P0 { 
			get {
				return new Vector2d(X0, Y0);
			}
		}

		/// <summary>
		/// Gets the top right corner.
		/// </summary>
		/// <value>The top right corner.</value>
		[JsonConverter(typeof(OTKVector2dConverter))]
		public Vector2d P1 {
			get {
				return new Vector2d(X1, Y1);
			}
		}

		// XXX: These synonyms are a little untidy but are convenient.
		public double Bottom {
			get {
				return Y0;
			}
		}

		public double Top {
			get {
				return Y1;
			}
		}


		public double Left {
			get {
				return X0;
			}
		}

		public double Right {
			get {
				return X1;
			}
		}

		public BBox(double x0, double y0, double x1, double y1)
		{
			// We ensure that P0 is always the bottom-left corner,
			// P1 is always the top-right corner.
			X0 = Math.Min(x0, x1);
			Y0 = Math.Min(y0, y1);
			X1 = Math.Max(x0, x1);
			Y1 = Math.Max(y0, y1);
		}

		public bool IntersectsBBox(BBox other)
		{
			return ((Left <= other.Left && other.Left <= Right) || (other.Left <= Left && Left <= other.Right)) &&
			((Bottom <= other.Bottom && other.Bottom <= Top) || (other.Bottom <= Bottom && Bottom <= other.Top));
		}

		public BBox Intersection(BBox other)
		{
			var x0 = Math.Max(Left, other.Left);
			var y0 = Math.Max(Bottom, other.Bottom);
			var x1 = Math.Min(Right, other.Right);
			var y1 = Math.Min(Top, other.Top);
			if (x0 <= x1 && y0 <= y1) {
				return new BBox(x0, y0, x1, y1);
			} else {
				return null;
			}
		}

		public bool ContainsPoint(Vector2d point)
		{
			return (Left <= point.X && point.X <= Right) &&
			(Bottom <= point.Y && point.Y <= Top);
		}

		public Vector2d Center()
		{
			return new Vector2d(X0 + (0.5 * Dx), X0 + (0.5 * Dy));
		}

		public BBox ShrinkAbsoluteBorder(Vector2d amount)
		{
			var dx = SMath.Clamp(amount.X, 0, 0.5 * Dx);
			var dy = SMath.Clamp(amount.Y, 0.0, 0.5 * Dy);
			return new BBox(X0 + dx, Y0 + dy, X1 + dx, Y1 + dy);
		}

		public Vector2d ClampVector(Vector2d vec)
		{
			return new Vector2d(SMath.Clamp(vec.X, Left, Right), SMath.Clamp(vec.Y, Bottom, Top));
		}

		public BBox Translated(Vector2d delta)
		{
			return new BBox(X0 + delta.X, Y0 + delta.Y, X1 + delta.X, Y1 + delta.Y);
		}

		public void Translate(Vector2d delta)
		{
			X0 += delta.X;
			X1 += delta.X;
			Y0 += delta.Y;
			Y1 += delta.Y;
		}

		public override string ToString()
		{
			return string.Format("BBox({0}, {1}, {2}, {3})", X0, Y0, X1, Y1);
		}
	}

	public class Line
	{
		public double X0;
		public double Y0;
		public double X1;
		public double Y1;

		[JsonConstructor]
		public Line(double x0, double y0, double x1, double y1)
		{
			X0 = x0;
			Y0 = y0;
			X1 = x1;
			Y1 = y1;
		}

		public Line(Vector2d p0, Vector2d p1) : this(p0.X, p0.Y, p1.X, p1.Y)
		{
		}

		public double Length()
		{
			var dx = X1 - X0;
			var dy = Y1 - Y0;
			return Math.Sqrt(dx * dx + dy * dy);
		}

		public Vector2d Center()
		{
			return new Vector2d(X0 + 0.5 * (X1 - X0), Y0 + 0.5 * (Y1 - Y0));
		}
	}

	public class Intersection
	{
		public Vector2d Contact;
		public Vector2d Normal;
		public double Protrusion;
		public double Intrusion;
		public double FlatCW;
		public double FlatCCW;

		public Intersection(Vector2d contact, Vector2d? normal = null, double protrusion = 0.0,
		                    double intrusion = 0.0, double flatCW = 0.0, double flatCCW = 0.0)
		{
			Contact = contact;
			Normal = normal ?? Vector2d.Zero;
			Protrusion = protrusion;
			Intrusion = intrusion;
			FlatCW = flatCW;
			FlatCCW = flatCCW;
		}

		public Intersection Inverse()
		{
			return new Intersection(Contact, -Normal, Intrusion, Protrusion, FlatCCW, FlatCW);
		}

		public override string ToString()
		{
			return string.Format("Intersection(contact {0}, normal {1}, protrusion {2}, intrusion {3}, flatCW {4}, flatCCW {5})",
				Contact, Normal, Protrusion, Intrusion, FlatCW, FlatCCW);
		}
	}

	public abstract class Geom
	{
		// XXX: This should return the same type as itself in all children...
		// Is there some handy way of doing that?
		public abstract Geom Translated(Vector2d delta);

		public abstract void Translate(Vector2d delta);


		public abstract Intersection Intersect(Geom other);

		public abstract Intersection IntersectLine(Line other);

		public abstract Intersection IntersectBBox(BBox other);
	}
	// TODO: Implement this, and add a test for seralizing it when you do
	public class LineGeom : Geom
	{
		public override Geom Translated(Vector2d delta)
		{
			return null;
		}

		public override void Translate(Vector2d delta)
		{
			return;
		}

		public override Intersection Intersect(Geom other)
		{
			//return other.IntersectLine(this);
			return null;
		}

		public override Intersection IntersectLine(Line other)
		{
			return null;
		}

		public override Intersection IntersectBBox(BBox other)
		{
			return null;
		}
	}

	public class BoxGeom : Geom
	{
		[JsonProperty]
		BBox bbox;

		public BoxGeom(double x0, double y0, double x1, double y1)
		{
			bbox = new BBox(x0, y0, x1, y1);
		}

		[JsonConstructor]
		public BoxGeom(BBox bbox)
		{
			this.bbox = bbox;
		}

		public override Geom Translated(Vector2d delta)
		{
			return new BoxGeom(bbox.Translated(delta));
		}

		public override void Translate(Vector2d delta)
		{
			this.bbox = bbox.Translated(delta);
		}

		public override Intersection Intersect(Geom other)
		{
			return other.IntersectBBox(this.bbox);
		}
		// TODO: Implement this.
		public override Intersection IntersectLine(Line other)
		{
			return null;
		}

		// BUGGO: Yikes this suddenly broke dramatically when I re-ordered the
		// constructor arguments for BBox's.  I thought I fixed all the instances
		// where that mattered though!
		public override Intersection IntersectBBox(BBox other)
		{
			var intersectionBBox = bbox.Intersection(other);
			if (intersectionBBox == null) {
				return null;
			}
			var contact = intersectionBBox.Center();
			// XXX: rather kludgy and approximate; should take into account starting velocity, etc.
			var dx0 = contact.X - other.Left;
			var dx1 = other.Right - contact.X;
			var dx = Math.Min(dx0, dx1);
			var xside = dx0 < dx1 ? -Vector2d.UnitX : Vector2d.UnitX;
			var dy0 = contact.Y - other.Bottom;
			var dy1 = other.Top - contact.Y;
			var dy = Math.Min(dy0, dy1);
			var yside = dy0 < dy1 ? Vector2d.UnitY : -Vector2d.UnitY;
			var ds = Math.Min(dx, dy);
			var sideNormal = dx < dy ? xside : yside;
			var flat = dx < dy ? intersectionBBox.Dy : intersectionBBox.Dx;
			return new Intersection(contact, sideNormal, 0.5 * ds, -0.5 * ds, flat, -flat);
		}

		public override string ToString()
		{
			return string.Format("BoxGeom({0})", bbox);
		}
	}
	// XXX: Should we have a CircleGeom too?  It makes life harder, though.
}

