using System;
using System.Collections;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	public class RenderState : Component, IComparable<RenderState>
	{
		public IRenderer Renderer { get; set; }

		readonly long OrderingNumber;

		public RenderState(Actor act, string renderclass) : base(act)
		{
			Renderer = Resources.TheResources.GetRenderer(renderclass);
			OrderingNumber = Util.GetSerial();
		}

		public int CompareTo(RenderState other)
		{
			return OrderingNumber.CompareTo(other.OrderingNumber);
		}
	}

	public class ModelRenderState : RenderState
	{
		public VertexArray Model;

		public ModelRenderState(Actor act, VertexArray model) : base(act, "StaticModelRenderer")
		{
			Log.Assert(model != null);
			Model = model;
		}
	}

	public class BillboardRenderState : RenderState
	{
		public Texture Texture;
		public float Rotation;
		public Vector2 Scale;

		public BillboardRenderState(Actor act, Texture texture, float rotation = 0.0f, Vector2? scale = null) : base(act, "BillboardRenderer")
		{
			Log.Assert(texture != null);
			Texture = texture;
			Rotation = rotation;
			Scale = scale ?? Vector2.One;
		}
	}

	public class GUIRenderState : RenderState
	{
		public Texture Texture;
		public float Rotation;
		public Vector2 Scale;

		public GUIRenderState(Actor act, Texture texture, float rotation = 0.0f, Vector2? scale = null)
			: base(act, "GUIRenderer")
		{
			Log.Assert(texture != null);
			Texture = texture;
			Rotation = rotation;
			Scale = scale ?? Vector2.One;
		}
	}

	/// <summary>
	/// Renders an animated, textured billboard.
	/// </summary>
	public class SpriteRenderState : RenderState
	{
		public TextureAtlas Atlas { get; set; }

		public List<Animation> Animations { get; set; }

		public int CurrentAnim { get; set; }

		public float Rotation { get; set; }

		public Vector2 Scale { get; set; }

		public SpriteRenderState(Actor act, TextureAtlas atlas, IEnumerable<Animation> anim, float rotation = 0.0f, Vector2? scale = null) : base(act, "SpriteRenderer")
		{
			Log.Assert(atlas != null);
			Log.Assert(anim != null);
			Atlas = atlas;
			Animations = new List<Animation>(anim);
			Log.Assert(Animations.Count > 0);
			CurrentAnim = 0;
			Rotation = rotation;
			Scale = scale ?? Vector2.One;

			HandledEvents = EventType.OnUpdate;
		}

		public SpriteRenderState(Actor owner, TextureAtlas atlas, Animation anim, float rotation = 0.0f, Vector2? scale = null)
			: this(owner, atlas, new Animation[] { anim }, rotation, scale)
		{
		}

		/// <summary>
		/// Returns UV coordinates for this sprite's current animation frame.
		/// </summary>
		/// <returns>Vector4(x0, y0, w, h)</returns>
		public Vector4 GetSourceTexcoords()
		{
			var anim = Animations[CurrentAnim];
			var frame = anim.Frame;
			var x = Atlas.OffsetX(frame);
			var y = Atlas.OffsetY(CurrentAnim);
			var w = Atlas.ItemWidth();
			var h = Atlas.ItemHeight();
			return new Vector4((float)x, (float)y, (float)w, (float)h);
		}

		public void AddAnimation(Animation anim)
		{
			Animations.Add(anim);
		}

		public override void OnUpdate(object sender, FrameEventArgs args)
		{
			var dt = args.Time;
			Animations[CurrentAnim].Update(dt);
		}
	}

	public class ParticleRenderState : RenderState
	{
		/* XXX
         * So I am consolidating the particle system to see if I can fit 
        the particle system in the render system. So I do not know the impact it can have
         * on render speed
         */
		public ParticleEmitter emitter;
		public float Rotation;
		public Vector2 Scale;
        public Texture texture;

		public ParticleRenderState(Actor act, Texture tex, double emitDelay = 0.1, float rotation = 0.0f, Vector2? scale = null)
			: base(act, "ParticleRenderer")
		{
            texture = tex;
            Log.Assert(texture != null);
			emitter = new ParticleEmitter(emitDelay);
			Rotation = rotation;
			Scale = scale ?? Vector2.One;
		}

	}

	public class RenderBatch<T> where T : RenderState
	{
		public SortedSet<T> RenderState;

		public RenderBatch()
		{
			RenderState = new SortedSet<T>();
		}

		public void Add(RenderState r)
		{
			T fml = r as T;
			if (fml == null) {
				Log.Message("Something went screwy adding item to a renderbatch; expected {0}, got {1}", typeof(T), r);
			} else {
				RenderState.Add(fml);
			}
		}

		public void Remove(RenderState r)
		{
			T fml = r as T;
			if (fml == null) {
				Log.Message("Something went screwy removing item from a renderbatch; expected {0}, got {1}", typeof(T), r);
			} else {
				RenderState.Remove(fml);
			}
		}
		// XXX: Are these right?
		public void Add(T r)
		{
			RenderState.Add(r);
		}

		public void Remove(T r)
		{
			RenderState.Remove(r);
		}
	}

	/// <summary>
	/// A class to manage drawing a heterogenous set of actors.
	/// This class keeps track of all Renderers, which then keep track of which actors they draw.
	/// It also handles Z ordering in the Renderers and the postprocessing.
	/// pipeline.
	/// </summary>
	public class RenderManager
	{
		//SortedDictionary<IRenderer, SortedSet<Actor>> Renderers;
		SortedSet<IRenderer> Renderers;
		PostprocPipeline postproc;

		public RenderManager(int width, int height)
		{
			Renderers = new SortedSet<IRenderer>();
			foreach (var renderclass in Util.GetImplementorsOf(typeof(IRenderer))) {
				// Remember to get the cached renderer from the resources system here.
				if (!renderclass.ContainsGenericParameters && !renderclass.IsAbstract) {
					var renderer = Resources.TheResources.GetRenderer(renderclass.Name);
					Renderers.Add(renderer);
				}
			}

			postproc = new PostprocPipeline(width, height);
			var ppShader = Resources.TheResources.GetShader("postproc");
			postproc.AddStep(ppShader);
			//postproc.AddStep(new GlowFilter(width, width));
			var fxaaShader = Resources.TheResources.GetShader("fxaa");
			postproc.AddStep(fxaaShader);
		}

		public void Add(Actor act)
		{
			//Console.WriteLine("Got renderer {0} for actor {1}", renderer, act);
			var rs = act.RenderState;
			rs.Renderer.Add(rs);
			//foreach (var r in Renderers) {
			//	Console.WriteLine("Renderer: {0}, total {1}", r, Renderers.Count);
			//}
		}

       
		public void Remove(Actor act)
		{
			var rs = act.RenderState;
			rs.Renderer.Remove(rs);
		}

		public void Render(ViewManager view)
		{
			Action thunk = () => {
				foreach (var renderer in Renderers) {
					//Console.WriteLine("Rendering {0} with {1}?", actors, renderer);
					renderer.Render(view);
				}
			};

			postproc.RenderWith(thunk);
		}

		public void Resize(int width, int height)
		{
			postproc.Resize(width, height);
		}
	}

	public enum ZOrder
	{
		BG = 00,
		Terrain = 10,
		FG = 20,
		GUI = 30,
	}

	public interface IRenderer : IComparable<IRenderer>
	{

		ZOrder zOrder { get; set; }

		long serial { get; set; }

		void Render(ViewManager view);

		void Add(RenderState r);

		void Remove(RenderState r);
	}

	/// <summary>
	/// A Renderer is an object that does the drawing for a specific type of Actor.
	/// This is an abstract base class; all children from this will be automatically
	/// added to the list of available Renderer's.  Each Renderer exists precisely once,
	/// and is instantiated at load time, so any resources it creates (such as loading meshes
	/// or images) and holds on to are loaded only once and are disposed of properly.
	/// 
	/// Note that these should not keep Actor-level state around.  If it needs some data per-Actor,
	/// the Actors need to be able to provide it.
	/// </summary>
	public abstract class Renderer<T> : IRenderer where T : RenderState
	{
		public ZOrder zOrder { get; set; }

		public long serial { get; set; }

		protected GLDiscipline discipline;
		protected Shader shader;
		protected RenderBatch<T> RenderBatch;

		public Renderer()
		{
			zOrder = ZOrder.FG;
			serial = Util.GetSerial();
			RenderBatch = new RenderBatch<T>();
			discipline = GLDiscipline.DEFAULT;
		}

		public virtual void RenderStart()
		{
			Graphics.TheGLTracking.SetDiscipline(discipline);
			Graphics.TheGLTracking.SetShader(shader);
		}

		public virtual void Render(ViewManager view)
		{
			RenderStart();
			foreach (var rs in RenderBatch.RenderState) {
				RenderOne(view, rs);
			}
		}

		/// <summary>
		/// Must be overriden in subclasses.
		/// </summary>
		/// <param name="view">View.</param>
		/// <param name="r">RenderState</param>
		protected virtual void RenderOne(ViewManager view, T r)
		{

		}

		public void Add(RenderState r)
		{
			RenderBatch.Add(r);
		}

		public void Remove(RenderState r)
		{
			RenderBatch.Remove(r as T);
		}

		public void Add(T r)
		{
			RenderBatch.Add(r);
		}

		public void Remove(T r)
		{
			RenderBatch.Remove(r);
		}

		/// <summary>
		/// Renderers sort themselves first by their Z order, then in an arbitrary but consistent order.
		/// This way we can all just put them in a SortedDictionary and have them draw in order without
		/// an additional data structure to draw one layer at a time.
		/// </summary>
		/// <returns>-1 for less than, 0 for equal, 1 for greater than.</returns>
		/// <param name="other">Object to compare to.</param>
		// OPT: I feel like it should be possible to do this without the if.
		public int CompareTo(IRenderer other)
		{
			if (other.zOrder != zOrder) {
				return zOrder.CompareTo(other.zOrder);
			} else {
				return other.serial.CompareTo(serial);
			}
		}
	}

	/// <summary>
	/// A renderer that draws nothing.
	/// </summary>
	public class NullRenderer : Renderer<RenderState>
	{
	}

	public class TestRenderer : Renderer<RenderState>
	{
		VertexArray model;

		public TestRenderer() : base()
		{
			model = Resources.TheResources.GetModel("TestModel");
			shader = Resources.TheResources.GetShader("default");
			discipline = GLDiscipline.DEFAULT;
		}

		protected override void RenderOne(ViewManager view, RenderState r)
		{
			//Console.WriteLine("Drawing actor");
			var pos = new Vector2((float)r.Owner.Body.Position.X, (float)r.Owner.Body.Position.Y);
			var transform = new Transform(pos, 0.0f);
			var mat = transform.TransformMatrix(view.ProjectionMatrix);
			shader.UniformMatrix("projection", mat);
			model.Draw();
		}
	}

	public class StaticModelRenderer : Renderer<ModelRenderState>
	{
		public StaticModelRenderer() : base()
		{
			shader = Resources.TheResources.GetShader("default");
			discipline = GLDiscipline.DEFAULT;
		}

		protected override void RenderOne(ViewManager view, ModelRenderState r)
		{
			var pos = new Vector2((float)r.Owner.Body.Position.X, (float)r.Owner.Body.Position.Y);
			var transform = new Transform(pos, 0.0f);
			var mat = transform.TransformMatrix(view.ProjectionMatrix);
			shader.UniformMatrix("projection", mat);
			r.Model.Draw();
		}
	}

	public class BillboardRenderer : Renderer<BillboardRenderState>
	{
		VertexArray billboard;

		public BillboardRenderer() : base()
		{
			shader = Resources.TheResources.GetShader("default-tex");
			discipline = GLDiscipline.DEFAULT;
			billboard = Resources.TheResources.GetModel("Billboard");
		}

		protected override void RenderOne(ViewManager view, BillboardRenderState r)
		{
			// BUGGO: Is the billboard not centered here?
			var pos = new Vector2((float)r.Owner.Body.Position.X, (float)r.Owner.Body.Position.Y);
			var transform = new Transform(pos, r.Rotation, r.Scale);
			var mat = transform.TransformMatrix(view.ProjectionMatrix);
			shader.UniformMatrix("projection", mat);
			// This is, inconveniently, not the texture handle but in fact the texture unit offset.
			shader.Uniformi("tex", 0);
			r.Texture.Enable();
			billboard.Draw();
			r.Texture.Disable(); 
		}
	}

	public class GUIRenderer : Renderer<GUIRenderState>
	{
		VertexArray billboard;

		public GUIRenderer()
			: base()
		{
			shader = Resources.TheResources.GetShader("default-tex");
			discipline = GLDiscipline.DEFAULT;
			billboard = Resources.TheResources.GetModel("Billboard");
		}

		protected override void RenderOne(ViewManager view, GUIRenderState r)
		{
			// BUGGO: Is the billboard not centered here?
			var pos = new Vector2((float)r.Owner.Body.Position.X, (float)r.Owner.Body.Position.Y);
			var transform = new Transform(pos, r.Rotation, r.Scale);
			var mat = transform.TransformMatrix(ProjectionMatrix());
			shader.UniformMatrix("projection", mat);
			// This is, inconveniently, not the texture handle but in fact the texture unit offset.
			shader.Uniformi("tex", 0);
			r.Texture.Enable();
			billboard.Draw();
			r.Texture.Disable();
		}

		Matrix4 ProjectionMatrix()
		{
			float width = Util.LogicalScreenWidth;
			float height = width; 
			//=Util.LogicalScreenWidth / Options.AspectRatio;
			//Need to grab 
			//Vector2 VisibleSize = new Vector2(width, height);
			// XXX: Right now these values are pretty arbitrary.
			float ZNear = 0.0f;
			float ZFar = 10.0f;
			return Matrix4.CreateOrthographic(width, height, ZNear, ZFar);
		}
	}

	public class SpriteRenderer : Renderer<SpriteRenderState>
	{
		VertexArray billboard;

		public SpriteRenderer() : base()
		{
			shader = Resources.TheResources.GetShader("default-sprite");
			discipline = GLDiscipline.DEFAULT;
			billboard = Resources.TheResources.GetModel("Billboard");
		}

		protected override void RenderOne(ViewManager view, SpriteRenderState r)
		{
			var pos = new Vector2((float)r.Owner.Body.Position.X, (float)r.Owner.Body.Position.Y);
			var transform = new Transform(pos, r.Rotation, r.Scale);
			var mat = transform.TransformMatrix(view.ProjectionMatrix);
			shader.UniformMatrix("projection", mat);
			shader.Uniformi("tex", 0);
			var coords = r.GetSourceTexcoords();
			shader.Uniformf("atlasCoords", coords.X, coords.Y, coords.Z, coords.W);
			r.Atlas.Enable();
			billboard.Draw();
			r.Atlas.Disable();
		}
	}

	public class SwirlyTestRenderer : Renderer<RenderState>
	{
		readonly Color4[] Colors = new Color4[] {
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
			new Color4(1.0f, 0f, 0f, 0.3f),
			new Color4(1.0f, 0.5f, 0f, 0.3f),
		};
		const double startSize = 50;
		const double scaleFactor = 0.9;
		List<VertexArray> Rects;
		double time;

		public SwirlyTestRenderer() : base()
		{
			shader = Resources.TheResources.GetShader("default");
			discipline = GLDiscipline.DEFAULT;

			Rects = new List<VertexArray>();
			var size = startSize;
			foreach (var color in Colors) {
				var rect = Starmaze.Content.Images.FilledRectCenter(0, 0, size, size, color);
				Rects.Add(rect);
				size *= scaleFactor;
			}

			time = 0.0;
		}

		protected override void RenderOne(ViewManager view, RenderState r)
		{
			time += 0.0005;
			for (int i = 0; i < Rects.Count; i++) {
				var pos = Vector2.Zero;
				var rot = (float)Math.Sin((time * i));
				var transform = new Transform(pos, rot);
				var mat = transform.TransformMatrix(view.ProjectionMatrix);
				shader.UniformMatrix("projection", mat);
				Rects[i].Draw();
			}

		}
	}

	public class TextRenderer : Renderer<RenderState>
	{
		public Texture tex;
		VertexArray billboard;

		public TextRenderer() : base()
		{
			shader = Resources.TheResources.GetShader("default-tex");
			discipline = GLDiscipline.DEFAULT;
			tex = Resources.TheResources.GetTexture("playertest");
			billboard = Resources.TheResources.GetModel("Billboard");

		}

		public void RenderText(ViewManager view, Vector2 _pos, Vector2 _scale)
		{
			var pos = _pos;
			var transform = new Transform(pos, 0.0f, _scale);
			var mat = transform.TransformMatrix(view.ProjectionMatrix);
			shader.UniformMatrix("projection", mat);
			// This is, inconveniently, not the texture handle but in fact the texture unit offset.
			shader.Uniformi("tex", 0);
			tex.Enable();
			billboard.Draw();
			tex.Disable();
		}
	}

	/// <summary>
	/// An object that draws a Particle List.
	/// </summary>
	public class ParticleRenderer : Renderer<ParticleRenderState>
	{
		VertexArray model;

		public ParticleRenderer()
		{
			shader = Resources.TheResources.GetShader("particle-default");
            model = Resources.TheResources.GetModel("Billboard");
		}


		protected override void RenderOne(ViewManager view, ParticleRenderState r)
		{

           /* var pos = new Vector2((float)r.Owner.Body.Position.X, (float)r.Owner.Body.Position.Y);
            var transform = new Transform(pos, r.Rotation, r.Scale);
            var mat = transform.TransformMatrix(view.ProjectionMatrix);
            */
            //give each particle its own texture and apply the shader (enable, disable it)
            foreach (var p in r.emitter.Particles)
            {
                var pos = new Vector2((float)p.Position.X, (float)p.Position.Y);
                var transform = new Transform(pos, r.Rotation, r.Scale);
                var mat = transform.TransformMatrix(view.ProjectionMatrix);

                shader.UniformMatrix("projection", mat);
                shader.Uniformf("offset", 0f, 0);
                shader.Uniformf("colorOffset", 1f, 0f, 0f, 1f);
                r.texture.Enable();
                model.Draw();
                r.texture.Disable();
            }

			//array.Draw();
		}

        /*
            foreach (var p in group.Particles)
            {
                var pos = new Vector2((float)p.Position.X, (float)p.Position.Y);
                shader.Uniformf("offset", (float)p.Position.X, (float)p.Position.Y);
                shader.Uniformf("colorOffset", p.Color.R, p.Color.G, p.Color.B, p.Color.A);
                array.Draw();
            }
           
        protected override void RenderOne(ViewManager view, SpriteRenderState r)
        {
            var pos = new Vector2((float)r.Owner.Body.Position.X, (float)r.Owner.Body.Position.Y);
            var transform = new Transform(pos, r.Rotation, r.Scale);
            var mat = transform.TransformMatrix(view.ProjectionMatrix);
            shader.UniformMatrix("projection", mat);
            shader.Uniformi("tex", 0);
            var coords = r.GetSourceTexcoords();
            shader.Uniformf("atlasCoords", coords.X, coords.Y, coords.Z, coords.W);
            r.Atlas.Enable();
            billboard.Draw();
            r.Atlas.Disable();
        } */
    }
}

