using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Starmaze.Engine
{
	//	public interface IResourceSource<T>
	//	{
	//		string ResourceRoot { get; set; }
	//
	//		T Load(string resourceName);
	//      T LoadDynamic(string resourceName);
	//
	//      // Or should this just be an Enumerate method that returns all the resource names this thing can load?
	//		void Preload(Dictionary<string, T> cache);
	//	}
	/// <summary>
	/// The resource loader does three things:
	/// 1) Provide a uniform interface to access assets from anywhere,
	/// 2) Provide a resource cache so we don't load the same texture or such more than once
	/// 3) Provide a way to get a particular asset as quickly as possible once it's loaded.
	/// </summary>
	public class ResourceLoader : IDisposable
	{
		string ResourceRoot;
		Dictionary<string, IRenderer> RendererCache;
		Dictionary<string, Texture> TextureCache;
		Dictionary<string, Shader> ShaderCache;
		Dictionary<string, VertexArray> ModelCache;
		Dictionary<string, CachedSound> SoundCache;
		Dictionary<string, Texture> TextCache;
		Dictionary<string, JObject> JsonCache;

		public ResourceLoader()
		{
			// BUGGO: This should be initialized from GameOptions, maybe?
			var basePath = Util.BasePath("..");
			ResourceRoot = basePath;
			RendererCache = new Dictionary<string, IRenderer>();
			TextureCache = new Dictionary<string, Texture>();
			ShaderCache = new Dictionary<string, Shader>();
			ModelCache = new Dictionary<string, VertexArray>();
			SoundCache = new Dictionary<string, CachedSound>();
			TextCache = new Dictionary<string, Texture>();
			JsonCache = new Dictionary<string, JObject>();
		}

		/// <summary>
		/// The concept with the cache is not only that we don't want to load things multiple times,
		/// but that we want each access to be as quick as possible.  Things can *always* be pre-cached
		/// at game-load or level-load time, but we want each actual access to be as fast as possible.
		/// Hence using exceptions rather than TryGetValue in Get(); throwing down a try/catch block
		/// that doesn't get used is probably faster than a potentially-mispredicted branch with TryGetValue.
		/// <param name="cache">Cache dict.</param>
		/// <param name="loader">Loader function.</param>
		/// <param name="name">Asset name.</param>
		/// <typeparam name="TKey">Cache key</typeparam>
		/// <typeparam name="TVal">Cache value</typeparam>
		TVal Get<TKey,TVal>(Dictionary<TKey,TVal> cache, Func<TKey,TVal> loader, TKey name)
		{
			try {
				return cache[name];
			} catch (KeyNotFoundException) {
				try {
					var t = loader(name);
					cache.Add(name, t);
					return t;
				} catch {
					Log.Message("Error loading {0}!", name);
					throw;
				}
			}
		}

		/// <summary>
		/// Produces a mapping from string to instance of all of the subclasses of Renderer.
		/// Conveniently both preloads all Renderer's and makes an association of them to their
		/// name.  Using strings to refer to them is a _little_ grotty but reflects nicely.
		/// </summary>
		/// <returns>The render map.</returns>
		static void PreloadRenderers(Dictionary<string, IRenderer> rendererMap)
		{

			var subclasses = Util.GetImplementorsOf(typeof(IRenderer));
			// We go through all subclasses of Renderer, instantiate one of each, and 
			// associate each with its name, and that gets us the string -> Renderer mapping.
			Console.WriteLine("Preloading renderers...");
			foreach (Type subclass in subclasses) {
				try {
					Log.Message("Renderer: {0}", subclass);
					if (!subclass.ContainsGenericParameters && !subclass.IsAbstract) {
						var renderer = (IRenderer)Activator.CreateInstance(subclass);
						rendererMap.Add(subclass.Name, renderer);
					}
				} catch (TargetInvocationException e) {
					// The renderer constructor threw an exception
					throw new InvalidProgramException(string.Format("Unable to preload renderer {0}\n{1}", subclass, e.InnerException), e.InnerException);
				}
			}

/*
			var r1 = new SpriteRenderer();
			var r2 = new StaticModelRenderer();
			var r3 = new SwirlyTestRenderer();
			var r4 = new TestRenderer();
			var r6 = new TexTestRenderer();
			var rs = new IRenderer[] {
				r1, r2, r3, r4,  r6
			};

			foreach (var r in rs) {
				var t = r.GetType();
				Log.Message("Preloading renderer: {0}", t.Name);
				rendererMap.Add(t.Name, r);
			}
			*/
		}

		public IRenderer GetRenderer(string r)
		{
			// We don't use the ResourceLoader.Get function here 'cause all renderers
			// are preloaded and associating a string to a renderer is a little squirrelly.
			// So we don't actually have a LoadRenderer function.
			Log.Assert(RendererCache.ContainsKey(r), "Renderer cache does not contain key {0}", r);
			return RendererCache[r];
		}

		public Texture GetTexture(string r)
		{
			return Get(TextureCache, LoadTexture, r);
		}

		Texture LoadTexture(string file)
		{
			var resourceNameParts = file.Split(new [] { ':' }, 2);
			if (resourceNameParts.Length > 1 && resourceNameParts[0] == "dynamic") {
				var t = typeof(Starmaze.Content.Images);
				var method = t.GetMethod(resourceNameParts[1]);
				Log.Message("Loading dynamic texture {0}", method.Name);
				var tex = (Texture)method.Invoke(null, null);
				return tex;
			} else {
				var fullPath = System.IO.Path.Combine(ResourceRoot, "images", file + ".png");
				Log.Message("Loading image {0}", fullPath);
				Bitmap bitmap = new Bitmap(fullPath);
				var tex = new Texture(bitmap);
				return tex;
			}
		}

		public Texture GetStringTexture(string r)
		{
			return Get(TextCache, LoadStringTexture, r);
		}

		Texture LoadStringTexture(string s)
		{
			Log.Message("Loading string texture {0}", s);
			return TextDrawer.RenderString(s, Color4.White);
		}

		public Shader GetShader(string r)
		{
			return Get(ShaderCache, LoadShader, r);
		}

		Shader LoadShader(string name)
		{
			var fullPath = System.IO.Path.Combine(ResourceRoot, "shaders", name);
			Log.Message("Loading shader {0}", fullPath);
			var vertData = File.ReadAllText(fullPath + ".vert");
			var fragData = File.ReadAllText(fullPath + ".frag");
			return new Shader(vertData, fragData);
		}

		public VertexArray GetModel(string name)
		{
			return Get(ModelCache, LoadModel, name);
		}
		// XXX: Dependency inversion here; should Content/Images.cs be elsewhere, or defined differently?
		// XXX: Can we mark certain things as uncachable?
		VertexArray LoadModel(string name)
		{
			var t = typeof(Starmaze.Content.Images);
			var method = t.GetMethod(name);
			Log.Message("Loading model {0}", method.Name);
			var model = (VertexArray)method.Invoke(null, null);
			return model;
		}
		//		Animation LoadAnimation(JArray json)
		//		{
		//			var l = new List<double>();
		//			foreach (var item in json) {
		//				l.Add(item.Value<double>());
		//			}
		//			var anim = new Animation(l.ToArray());
		//			return anim;
		//		}
		//
		//		TextureAtlas LoadTextureAtlas(JObject json)
		//		{
		//			var texname = json["texture"].Value<string>();
		//			var texture = Resources.TheResources.GetTexture(texname);
		//			var width = json["width"].Value<int>();
		//			var height = json["height"].Value<int>();
		//			return new TextureAtlas(texture, width, height);
		//		}
		public JObject GetJson(string file)
		{
			var fullPath = System.IO.Path.Combine(ResourceRoot, "config", file + ".cfg");
			Log.Message("Loading config file {0}", fullPath);
			return Get(JsonCache, LoadJson, fullPath);
		}

		JObject LoadJson(string file)
		{
			var json = JObject.Parse(File.ReadAllText(file));
			return json;
		}

		CachedSound LoadSound(string name)
		{
			//var req_samples = Resources.Options.SoundSampleRate;
			//var req_channels = Resources.Options.SoundChannels;

			var fullPath = System.IO.Path.Combine(ResourceRoot, "sounds", name);
			CachedSound sound = new CachedSound(fullPath);
			//sound = CorrectSoundFile(sound);
			return sound;

		}

		public CachedSound GetSound(string name)
		{
			return Get(SoundCache, LoadSound, name);
		}
		/*
		public ISampleProvider CorrectSoundFile(ISampleProvider soundIn)
		{

			var resampler = new WdlResamplingSampleProvider(soundIn, Resources.Options.SoundSampleRate);
			return (ISampleProvider)resampler;

		}*/

		/// <summary>
		/// Preload any resources that take a long time to load; ie, all of them.
		/// This is called AFTER the Resources object is already created, so that for instance
		/// the resources loaded in it can call upon other resources and it won't freak out
		/// due to Resources.TheResources not existing.
		/// </summary>
		public void Preload()
		{
			PreloadRenderers(RendererCache);
		}

		private bool disposed = false;

		~ResourceLoader()
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
				foreach (var kv in ShaderCache) {
					var shader = kv.Value;
					Log.Message("Freeing shader {0}", kv.Key);
					shader.Dispose();
				}

				foreach (var kv in TextureCache) {
					var texture = kv.Value;
					Log.Message("Freeing texture {0}", kv.Key);
					texture.Dispose();
				}

				foreach (var kv in ModelCache) {
					var model = kv.Value;
					Log.Message("Freeing model {0}", kv.Key);
					model.Dispose();
				}

				// XXX:
				// Disposing of Renderer's is a little sticky;
				// in a perfect world they would get all their stuff
				// from the ResourceLoader and thus all of it would be
				// managed already
				// But we have yet to see whether or not the world is that perfect.
				//foreach (var kv in RendererCache) {
				//var renderer = kv.Value;
				//renderer.Dispose();
				//}
			}
		}
	}

	/// <summary>
	/// A singleton class that handles resource loading and caching.
	/// </summary>
	public static class Resources
	{
		// Singleton pattern.
		// Except with explicit initialization because latency matters.
		// And explicit destruction because that matters too.
		static ResourceLoader _TheResources;
		public static GameOptions Options;
		public static Sound Sound;//XXXX: Sound might want to be it's own singleton instead
		public static ResourceLoader TheResources {
			get {
				Log.Assert(_TheResources != null, "Attempting to get null Resources object");
				return _TheResources;
			}
		}

		public static bool IsInitialized {
			get {
				return _TheResources != null;
			}
		}

		public static ResourceLoader Init(GameOptions GOptions)
		{
			if (_TheResources != null) {
				// XXX: Better exception type
				throw new Exception("Bogusly re-init'ing ResourceLoader");
			}
			_TheResources = new ResourceLoader();
			Options = GOptions;
			_TheResources.Preload();
			Sound = new Sound(); //XXXX: Sound might want to be it's own singleton instead
			return _TheResources;
		}

		public static void CleanupResources()
		{
			if (_TheResources == null) {
				// XXX: Better exception type
				throw new Exception("Bogusly cleaning up already-disposed ResourceLoader");
			} else {
				_TheResources.Dispose();
				_TheResources = null;
			}
		}
	}
}

