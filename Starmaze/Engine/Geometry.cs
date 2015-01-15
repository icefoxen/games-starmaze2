using System;
using OpenTK;

namespace Starmaze.Engine
{
	/// <summary>
	/// A bounding box.
	/// </summary>
	public class BBox
	{
		public double Left;
		public double Right;
		public double Bottom;
		public double Top;

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
		public Vector2d P0 { 
			get {
				return new Vector2d(Bottom, Left);
			}
		}

		/// <summary>
		/// Gets the top right corner.
		/// </summary>
		/// <value>The top right corner.</value>
		public Vector2d P1 {
			get {
				return new Vector2d(Top, Right);
			}
		}

		public BBox(double left, double right, double bottom, double top)
		{
			Left = left;
			Right = right;
			Bottom = bottom;
			Top = top;
		}

		public bool IntersectsBBox(BBox other)
		{
			return ((Left <= other.Left && other.Left <= Right) || (other.Left <= Left && Left <= other.Right)) &&
			((Bottom <= other.Bottom && other.Bottom <= Top) || (other.Bottom <= Bottom && Bottom <= other.Top));
		}

		public BBox Intersection(BBox other)
		{
			var bottom = Math.Max(Bottom, other.Bottom);
			var top = Math.Min(Top, other.Top);
			var left = Math.Max(Left, other.Left);
			var right = Math.Min(Right, other.Right);
			if (left <= right && bottom <= top) {
				return new BBox(left, right, top, bottom);
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
			return new Vector2d(Left + (0.5 * Dx), Bottom + (0.5 * Dy));
		}

		public BBox ShrinkAbsoluteBorder(Vector2d amount)
		{
			var dx = SMath.Clamp(amount.X, 0, 0.5 * Dx);
			var dy = SMath.Clamp(amount.Y, 0.0, 0.5 * Dy);
			return new BBox(Left + dx, Right - dx, Bottom + dy, Top - dy);
		}

		public Vector2d ClampVector(Vector2d vec)
		{
			return new Vector2d(SMath.Clamp(vec.X, Left, Right), SMath.Clamp(vec.Y, Bottom, Top));
		}

		public BBox Translate(Vector2d delta)
		{
			return new BBox(Left + delta.X, Right + delta.X, Bottom + delta.Y, Top + delta.Y);
		}
	}

	public class Line
	{
		public double X0;
		public double Y0;
		public double X1;
		public double Y1;

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
	}

	public abstract class Geom
	{
		// XXX: This should return the same type as itself in all children...
		// Is there some handy way of doing that?
		public virtual Geom Translate(Vector2d delta)
		{
			return null;
		}

		public virtual Intersection Intersect(Geom other)
		{
			Log.Assert(false);
			return null;
		}

		public virtual Intersection IntersectLine(Line other)
		{
			Log.Assert(false);
			return null;
		}

		public virtual Intersection IntersectBBox(BBox other)
		{
			Log.Assert(false);
			return null;
		}
	}

	public class LineGeom : Geom
	{
		public override Geom Translate(Vector2d delta)
		{
			return null;
		}

		public override Intersection Intersect(Geom other)
		{
			//return other.IntersectLine(this);
			return null;
		}
		// TODO: Implement this
		public override Intersection IntersectLine(Line other)
		{
			return null;
		}
		// TODO: Implement this
		public override Intersection IntersectBBox(BBox other)
		{
			return null;
		}
	}

	public class BoxGeom : Geom
	{
		BBox bbox;

		public BoxGeom(double left, double right, double bottom, double top)
		{
			bbox = new BBox(left, right, bottom, top);
		}

		public BoxGeom(BBox bbox)
		{
			this.bbox = bbox;
		}

		public override Geom Translate(Vector2d delta)
		{
			return new BoxGeom(bbox.Translate(delta));
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
			return new Intersection(contact, sideNormal, 0.5 * ds, 0.5 * ds, flat);
		}
	}
	// XXX: Should we have a CircleGeom too?  It makes life harder, though.
}

