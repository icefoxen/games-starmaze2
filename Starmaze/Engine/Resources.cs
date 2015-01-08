using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	public class ResourceLoader : IDisposable
	{

		string ResourceRoot;
		Dictionary<string, Renderer> RendererCache;
		Dictionary<string, Texture> TextureCache;
		Dictionary<string, Shader> ShaderCache;

		public ResourceLoader()
		{
			var basePath = Environment.GetEnvironmentVariable("STARMAZE_HOME");
			if (basePath == null) {
				// This gets the location of the .exe, essentially.
				basePath = AppDomain.CurrentDomain.BaseDirectory;
			}
			// BUGGO: Still a bit ugly, since in the current build system the .exe is not
			// in the resource path root.
			basePath = System.IO.Path.Combine(basePath, "..");
			ResourceRoot = basePath;
			RendererCache = new Dictionary<string, Renderer>();
			TextureCache = new Dictionary<string, Texture>();
			ShaderCache = new Dictionary<string, Shader>();
		}

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
					Console.WriteLine("Error loading {0}!", name);
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
		static void PreloadRenderers(Dictionary<string, Renderer> rendererMap)
		{
			var subclasses = Util.GetSubclassesOf(typeof(Renderer));
			// We go through all subclasses of Renderer, instantiate one of each, and 
			// associate each with its name, and that gets us the string -> Renderer mapping.
			foreach (Type subclass in subclasses) {
				var renderer = (Renderer)Activator.CreateInstance(subclass);
				rendererMap.Add(subclass.Name, renderer);
			}
		}

		public Renderer GetRenderer(string r)
		{
			// We don't use the ResourceLoader.Get function here 'cause all renderers
			// are preloaded and associating a string to a renderer is a little squirrelly.
			// So we don't actually have a LoadRenderer function.
			return RendererCache[r];
		}

		public Texture GetImage(string r)
		{
			return Get(TextureCache, LoadTexture, r);
		}

		Texture LoadTexture(string file)
		{
			var fullPath = System.IO.Path.Combine(ResourceRoot, "images", file + ".png");
			Log.Message("Loading image {0}", fullPath);
			Bitmap bitmap = new Bitmap(fullPath);
			var t = new Texture(bitmap);
			return t;
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

	/// <summary>
	/// A singleton class that handles resource loading and caching.
	/// </summary>
	public static class Resources
	{
		// Singleton pattern.
		// Except with explicit initialization because latency matters.
		// And explicit destruction because that matters too.
		static ResourceLoader _TheResources;

		public static ResourceLoader TheResources {
			get {
				Log.Assert(_TheResources != null);
				return _TheResources;
			}
		}

		public static ResourceLoader InitResources()
		{
			if (_TheResources != null) {
				// XXX: Better exception type
				throw new Exception("Bogusly re-init'ing ResourceLoader");
			}
			_TheResources = new ResourceLoader();
			_TheResources.Preload();
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

