using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	public class Shader : IDisposable
	{
		readonly int ProgramHandle;
		List<int> ShaderHandles;

		public Shader(string vertexProgram, string fragmentProgram)
		{
			ShaderHandles = new List<int>();
			ProgramHandle = GL.CreateProgram();
			createShader(vertexProgram, ShaderType.VertexShader);
			createShader(fragmentProgram, ShaderType.FragmentShader);
			link();
		}

		private bool disposed = false;

		~Shader()
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
				// This bit is only here in the unlikely event we need to do stuff
				// in the finalizer or override this in a child class or something, I guess.
				// But resource allocation is Important and Hard so I'm cleaving to the
				// recommended idiom in this.
			}
			// Clean up unmanaged resources
			foreach (var shaderhandle in ShaderHandles) {
				GL.DeleteShader(shaderhandle);
			}
			GL.DeleteProgram(ProgramHandle);
		}

		void createShader(string program, ShaderType type)
		{
			var shader = GL.CreateShader(type);
			ShaderHandles.Add(shader);
			GL.ShaderSource(shader, program);
			GL.CompileShader(shader);


			// Get compile status.
			int status;
			GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
			if (status != 1) {
				var log = GL.GetShaderInfoLog(shader);
				var msg = String.Format("Shader status {0}, info log: {1}", status, log);
				throw new Exception(msg);
			} else {
				// All is well, attach shader to program
				GL.AttachShader(ProgramHandle, shader);
			}
		}

		void link()
		{
			GL.LinkProgram(ProgramHandle);
			int status;
			GL.GetProgram(ProgramHandle, GetProgramParameterName.LinkStatus, out status);
			if (status != 1) {
				var log = GL.GetProgramInfoLog(ProgramHandle);
				var msg = String.Format("Shader program status {0}, info log: {1}", status, log);
				throw new Exception(msg);
			} else {
				// All is well
				;
			}
		}

		public void Enable()
		{
			GL.UseProgram(ProgramHandle);
		}

		public void Disable()
		{
			GL.UseProgram(0);
		}

		public void Uniformf(string name, float val1)
		{
			var loc = GL.GetUniformLocation(ProgramHandle, name);
			GL.Uniform1(loc, val1);
		}

		public void Uniformf(string name, float val1, float val2)
		{
			var loc = GL.GetUniformLocation(ProgramHandle, name);
			GL.Uniform2(loc, val1, val2);
		}

		public void Uniformf(string name, float val1, float val2, float val3)
		{
			var loc = GL.GetUniformLocation(ProgramHandle, name);
			GL.Uniform3(loc, val1, val2, val3);
		}

		public void Uniformf(string name, float val1, float val2, float val3, float val4)
		{
			var loc = GL.GetUniformLocation(ProgramHandle, name);
			GL.Uniform4(loc, val1, val2, val3, val4);
		}

		public void Uniformi(string name, int val1)
		{
			var loc = GL.GetUniformLocation(ProgramHandle, name);
			GL.Uniform1(loc, val1);
		}

		public void Uniformi(string name, int val1, int val2)
		{
			var loc = GL.GetUniformLocation(ProgramHandle, name);
			GL.Uniform2(loc, val1, val2);
		}

		public void Uniformi(string name, int val1, int val2, int val3)
		{
			var loc = GL.GetUniformLocation(ProgramHandle, name);
			GL.Uniform3(loc, val1, val2, val3);
		}

		public void Uniformi(string name, int val1, int val2, int val3, int val4)
		{
			var loc = GL.GetUniformLocation(ProgramHandle, name);
			GL.Uniform4(loc, val1, val2, val3, val4);
		}

		public void UniformMatrix(string name, Matrix4 matrix)
		{
			var loc = GL.GetUniformLocation(ProgramHandle, name);
			GL.UniformMatrix4(loc, false, ref matrix);
		}

		public void UniformMatrix(string name, Matrix4d matrix)
		{
			var loc = GL.GetUniformLocation(ProgramHandle, name);
			GL.UniformMatrix4(loc, false, ref matrix);
		}

		public int VertexAttributeLocation(string name)
		{
			return GL.GetAttribLocation(ProgramHandle, name);
		}
	}
}

