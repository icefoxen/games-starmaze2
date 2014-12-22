using System;
using System.Collections.Generic;

namespace Starmaze.Engine
{
	/// <summary>
	/// A singleton class that handles resource loading and caching.
	/// </summary>
	public class Resources
	{
		// Singleton pattern.
		// Except with explicit initialization because latency matters.
		private static Resources _TheResources;
		public static Resources TheResources {
			get {
				return _TheResources;
			}
		}
		public static Resources InitResources() {
			if(_TheResources != null) {
				throw new Exception("Bogusly re-init'ing Resources");
			}
			_TheResources = new Resources();
			return _TheResources;
		}

		string ResourceRoot;
		Dictionary<string, Renderer> RendererCache;
		Dictionary<string, object> ImageCache;
		Dictionary<string, Shader> ShaderCache;

		public Resources()
		{
			ResourceRoot = Environment.GetEnvironmentVariable("STARMAZE_HOME");
			if(ResourceRoot == null) {
				ResourceRoot = "..";
			}
			RendererCache = new Dictionary<string, Renderer>();
			ImageCache = new Dictionary<string, object>();
			ShaderCache = new Dictionary<string, Shader>();

			Preload();
		}

		public Renderer GetRenderer(string r)
		{
			try {
				return RendererCache[r];
			} catch(KeyNotFoundException) {
				var renderer = LoadRenderer(r);
				RendererCache.Add(r, renderer);
				return renderer;
			}
		}

		Renderer LoadRenderer(string r)
		{
			return null;
		}

		public object GetImage(string r)
		{
			try {
				return ImageCache[r];
			} catch(KeyNotFoundException) {
				var image = LoadImage(r);
				ImageCache.Add(r, image);
				return image;
			}
		}

		object LoadImage(string r)
		{
			return null;
		}

		public Shader GetShader(string r)
		{
			try {
				return ShaderCache[r];
			} catch(KeyNotFoundException) {
				var shader = LoadShader(r);
				ShaderCache.Add(r, shader);
				return shader;
			}
		}

		Shader LoadShader(string r)
		{
			return null;
		}

		// If any resources need pre-loading, do it here.
		void Preload()
		{
			return;
		}
	}
}

