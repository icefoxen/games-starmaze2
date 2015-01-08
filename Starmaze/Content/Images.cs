using System;
using Starmaze;
using Starmaze.Engine;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Content
{
	class Images
	{
		public static VertexArray TestModel()
		{
			var vertexData = new float[] {
				// Verts
				-0.5f, -0.5f, 0.0f,
				-0.5f, 0.5f, 0.0f,
				+0.5f, 0.5f, 0.0f,
				+0.5f, -0.5f, 0.0f,
			};
			var colorData = new float[] {
				// Colors
				1.0f, 0.0f, 0.0f, 1.0f,
				0.0f, 1.0f, 0.0f, 1.0f,
				0.0f, 0.0f, 1.0f, 1.0f,
				1.0f, 1.0f, 1.0f, 1.0f,
			};

			var shader = Resources.TheResources.GetShader("default");

			var v = new VertexAttributeArray[] {
				new VertexAttributeArray("position", vertexData, 3),
				new VertexAttributeArray("color", colorData, 4)
			};
			var indices = new uint[] { 0, 1, 2, 0, 2, 3 };
			return new VertexArray(shader, v, indices);
		}

		public static VertexArray TestModel2()
		{
			var shader = Resources.TheResources.GetShader("default");
			var mb = new Starmaze.Engine.ModelBuilder();
			mb.Circle(0, 0, 15, Color4.Green, numSegments: 16);
			var model = mb.Finish();
			var va = model.ToVertexArray(shader);
			return va;
		}
	}
}

