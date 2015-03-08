using System;
using System.Collections.Generic;
using Starmaze;
using Starmaze.Engine;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Content
{
	class Images
	{
		public static VertexArray TestModel()
		{
			var shader = Resources.TheResources.GetShader("default");
			var mb = new Starmaze.Engine.ModelBuilder();
			//mb.Circle(0, 0, 15, Color4.Green, numSegments: 16);
			//mb.Circle(20, 30, 35, Color4.Red, numSegments: 64);
			mb.Line(-10, -10, 10, -10, Color4.Green);
			mb.Arc(0, 0, 10, SMath.TAU / 6, Color4.Yellow, numSegments: 64);

			mb.RectCorner(10, 10, 20, 10, Color4.BlueViolet);
			mb.RectCenterFilled(-20, 20, 5, 20, Color4.Aquamarine);

			var verts = new List<LineArtVertex>();
			var r = new Random();
			for (int i = 0; i < 5; i++) {
				var pos = new Vector2d(Math.Sin(i), Math.Cos(i)) * ((i * 2) + 5);
				var color = new Color4((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), 1);
				verts.Add(new LineArtVertex(pos, color));
			}
			mb.PolygonFilled(verts);

			// Concave test polygon that doesn't draw right.
			/*
			var verts2 = new Vector2d[] {
				new Vector2d(-25, 0),
				new Vector2d(-10, 10),
				new Vector2d(0, 25),
				new Vector2d(10, 10),
				new Vector2d(25, 0),
				new Vector2d(10, -10),
				new Vector2d(0, -25),
				new Vector2d(-10, -10),
			};
			mb.PolygonUniformFilled(verts2, Color4.LimeGreen);
			*/

			mb.RectCenterOutlined(0, 30, 15, 5, Color4.White, Color4.Blue);

			mb.RectCenterOutlined(0, -30, 15, 5, Color4.Black, Color4.Red);

			mb.CircleFilled(30, 30, 5, Color4.IndianRed);
			mb.CircleOutlined(-30, -30, 15, Color4.NavajoWhite, Color4.Tomato, numSegments: 32);
			var model = mb.Finish();
			var va = model.ToVertexArray(shader);
			return va;
		}

		public static VertexArray Player()
		{
			var shader = Resources.TheResources.GetShader("default");
			var mb = new Starmaze.Engine.ModelBuilder();
			var pcol = Color4.Green;
			var radius = 5;
			var spokeLength = 9;
			mb.Circle(0, 0, radius, pcol);
			mb.Line(0, 0, spokeLength, 0, pcol);
			mb.Line(0, 0, -spokeLength, 0, pcol);
			mb.Line(0, 0, 0, spokeLength, pcol);
			mb.Line(0, 0, 0, -spokeLength, pcol);
			var model = mb.Finish();
			return model.ToVertexArray(shader);
		}

		public static VertexArray Particle()
		{
			var shader = Resources.TheResources.GetShader("particle-default");
			var mb = new Starmaze.Engine.ModelBuilder();
			mb.Circle(0, 0, 1, Color4.White, numSegments: 8);
			var model = mb.Finish();
			return model.ToVertexArray(shader);
		}

		public static VertexArray Billboard()
		{
			var shader = Resources.TheResources.GetShader("default-tex");
			var bb = new VertexList(VertexLayout.TextureVertex);
			bb.AddTextureVertex(
				new Vector2(-5, -5),
				Color4.White,
				new Vector2(0, 1)
			);
			bb.AddTextureVertex(
				new Vector2(-5, 5),
				Color4.White,
				new Vector2(0, 0)
			);
			bb.AddTextureVertex(
				new Vector2(5, 5),
				Color4.White,
				new Vector2(1, 0)
			);
			bb.AddTextureVertex(
				new Vector2(5, -5),
				Color4.White,
				new Vector2(1, 1)
			);
			var indices = new uint[] {
				0, 1, 2,
				0, 2, 3,
			};
			return new VertexArray(shader, bb, idxs: indices);
		}

		public static VertexArray FilledRectCenter(double cx, double cy, double w, double h, Color4 color)
		{
			float halfW = (float)(w / 2);
			float halfH = (float)(h / 2);
			var positions = new Vector2[] {
				new Vector2((float)cx - halfW, (float)cy - halfH),
				new Vector2((float)cx - halfW, (float)cy + halfH),
				new Vector2((float)cx + halfW, (float)cy + halfH),
				new Vector2((float)cx + halfW, (float)cy - halfH),
			};

			var indices = new uint[] {
				0, 1, 2,
				0, 2, 3,
			};

			var shader = Resources.TheResources.GetShader("default");
			var bb = new VertexList(VertexLayout.ColorVertex);
			foreach (var pos in positions) {
				bb.AddColorVertex(pos, color);
			}
			return new VertexArray(shader, bb, idxs: indices);
		}
	}
}

