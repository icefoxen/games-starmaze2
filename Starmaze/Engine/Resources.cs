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
	public class ResourceLoader
	{

		string ResourceRoot;
		Dictionary<string, Renderer> RendererCache;
		Dictionary<string, uint> ImageCache;
		Dictionary<string, Shader> ShaderCache;

		public ResourceLoader()
		{
			var basePath = Environment.GetEnvironmentVariable("STARMAZE_HOME");
			if (basePath == null) {
				basePath = AppDomain.CurrentDomain.BaseDirectory;
			}
			// BUGGO: Still a bit ugly.
			basePath = Path.Combine(basePath, "..");
			ResourceRoot = basePath;
			RendererCache = new Dictionary<string, Renderer>();
			ImageCache = new Dictionary<string, uint>();
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
		/// Conveneiently both preloads all Renderer's and makes an association of them to their
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
			return Get(RendererCache, LoadRenderer, r);
		}

		Renderer LoadRenderer(string r)
		{
			return null;
		}

		public uint GetImage(string r)
		{
			return Get(ImageCache, LoadImage, r);
		}

		uint LoadImage(string file)
		{
			var fullPath = Path.Combine(ResourceRoot, "images", file + ".png");
			Console.WriteLine("Loading image {0}", fullPath);
			// BUGGO: Copy-pasta'd from other source, needs verification
			Bitmap bitmap = new Bitmap(fullPath);
			if (!Util.IsPowerOf2(bitmap.Width) || !Util.IsPowerOf2(bitmap.Height)) {
				// XXX: FormatException isn't really the best here, buuuut...
				throw new FormatException("Texture sizes must be powers of 2!");
			}
			uint texture;
			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

			GL.GenTextures(1, out texture);
			GL.BindTexture(TextureTarget.Texture2D, texture);

			BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
			                                  ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
			              OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
			bitmap.UnlockBits(data);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

			return texture;
		}

		public Shader GetShader(string r)
		{
			return Get(ShaderCache, LoadShader, r);
		}

		Shader LoadShader(string name)
		{
			var fullPath = Path.Combine(ResourceRoot, "shaders", name);
			Console.WriteLine("Loading shader {0}", fullPath);
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
	}

	/// <summary>
	/// A singleton class that handles resource loading and caching.
	/// </summary>
	public static class Resources
	{
		// Singleton pattern.
		// Except with explicit initialization because latency matters.
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
	}
}

