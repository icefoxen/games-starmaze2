using System;
using OpenTK;

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

		public void CenterOn(Vector2 pos)
		{
			CenterOn(pos.X, pos.Y);
		}

		public void CenterOn(Vector2d pos)
		{
			CenterOn((float)pos.X, (float)pos.Y);
		}
	}

	/// <summary>
	/// A camera that follows a given Actor within given constraints, and moves smoothly to
	/// keep them in sight.
	/// </summary>
	// TODO: The stay-within-screen-bounds bit should be tested harder.
	public class FollowCam
	{
		public Actor Target { get; set; }

		Vector2d AimedAtPos;
		public Vector2d CurrentPos;
		Vector2d Border;
		double ScrollSpeed;

		double const BorderScale;

		public FollowCam(double width, double height)
		{
			AimedAtPos = Vector2d.Zero;
			CurrentPos = Vector2d.Zero;
			Border = new Vector2d(width, height) * BorderScale;

			ScrollSpeed = 2.5;
		}

		public FollowCam(Actor target, double width, double height) : this(width, height)
		{
			Target = target;
		}

		public void SnapTo(Vector2d pos)
		{
			AimedAtPos = pos;
			CurrentPos = pos;
		}

		public void Update(double dt)
		{
			var targetPos = Target.Body.Position;
			var delta = targetPos - CurrentPos;
			var offset = delta * ScrollSpeed * dt;

			CurrentPos += offset;

			var delta2 = targetPos - CurrentPos;

			// We have to test whether the *next* frame will be out of bounds and account for that;
			// if we do it on one of the current frame's numbers then once you get close to the corners
			// of the screen the view gets 'sucked' into them.
			if (delta2.X > Border.X) {
				CurrentPos.X = targetPos.X - Border.X;
			} else if (delta2.X < -Border.X) {
				CurrentPos.X = targetPos.X + Border.X;
			}
			if (delta2.Y > Border.Y) {
				CurrentPos.Y = targetPos.Y - Border.Y;
			} else if (delta2.Y < -Border.Y) {
				CurrentPos.Y = targetPos.Y + Border.Y;
			}
		}


	}
}

