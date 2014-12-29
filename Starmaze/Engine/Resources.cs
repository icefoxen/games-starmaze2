using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
			ResourceRoot = Environment.GetEnvironmentVariable("STARMAZE_HOME");
			if(ResourceRoot == null) {
				ResourceRoot = "../../..";
			}
			RendererCache = new Dictionary<string, Renderer>();
			ImageCache = new Dictionary<string, uint>();
			ShaderCache = new Dictionary<string, Shader>();

			Preload();
		}

		TVal Get<TKey,TVal>(Dictionary<TKey,TVal> cache, Func<TKey,TVal> loader, TKey name)
		{
			try {
				return cache[name];
			} catch(KeyNotFoundException) {
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
			// BUGGO: The path handling really needs to be better
			var fullPath = ResourceRoot + "/images/" + file + ".png";
			Console.WriteLine("Loading image {0}", fullPath);
			// BUGGO: Copy-pasta'd from other source, needs verification
			Bitmap bitmap = new Bitmap(fullPath);
			if(!Util.IsPowerOf2(bitmap.Width) || !Util.IsPowerOf2(bitmap.Height)) {
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
			// BUGGO: The path handling really needs to be better
			var fullPath = ResourceRoot + "/shaders/" + name;
			Console.WriteLine("Loading shader {0}", fullPath);
			var vertData = File.ReadAllText(fullPath + ".vert");
			var fragData = File.ReadAllText(fullPath + ".frag");
			return new Shader(vertData, fragData);
		}

		// If any resources need pre-loading, do it here.
		void Preload()
		{
			return;
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
				return _TheResources;
			}
		}
		public static ResourceLoader InitResources() {
			if(_TheResources != null) {
				// XXX: Better exception type
				throw new Exception("Bogusly re-init'ing ResourceLoader");
			}
			_TheResources = new ResourceLoader();
			return _TheResources;
		}
	}
}

