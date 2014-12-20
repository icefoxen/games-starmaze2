using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Starmaze.Engine
{
	public class Shader
	{
		int Handle;
		bool Linked;
		public const string DefaultVertShader = @"#version 120
uniform sampler2D fbo_texture;
void main(void) {
   gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
   gl_FrontColor = gl_Color;
   gl_TexCoord[0] = gl_MultiTexCoord0;
}
";
		public const string DefaultFragShader = @"#version 120
uniform sampler2D fbo_texture;
void main(void) {

  vec2 texcoord = gl_TexCoord[0].st;
  vec4 texColor = texture2D(fbo_texture, texcoord);
  gl_FragColor = texColor;
}
";

		public Shader(string vertexProgram, string fragmentProgram)
		{
			Handle = GL.CreateProgram();
			createShader(vertexProgram, ShaderType.VertexShader);
			createShader(fragmentProgram, ShaderType.FragmentShader);
			link();
		}

		void createShader(string program, ShaderType type)
		{
			var shader = GL.CreateShader(type);
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
				GL.AttachShader(Handle, shader);
			}
		}

		void link()
		{
			GL.LinkProgram(Handle);
			int status;
			GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out status);
			if (status != 1) {
				var log = GL.GetProgramInfoLog(Handle);
				var msg = String.Format("Shader program status {0}, info log: {1}", status, log);
				throw new Exception(msg);
			} else {
				// All is well
				;
			}
		}

		public void Enable()
		{
			GL.UseProgram(Handle);
		}

		public void Disable()
		{
			GL.UseProgram(0);
		}

		public void Uniformf(string name, float val1)
		{
			var loc = GL.GetUniformLocation(Handle, name);
			GL.Uniform1(loc, val1);
		}

		public void Uniformf(string name, float val1, float val2)
		{
			var loc = GL.GetUniformLocation(Handle, name);
			GL.Uniform2(loc, val1, val2);
		}

		public void Uniformf(string name, float val1, float val2, float val3)
		{
			var loc = GL.GetUniformLocation(Handle, name);
			GL.Uniform3(loc, val1, val2, val3);
		}

		public void Uniformf(string name, float val1, float val2, float val3, float val4)
		{
			var loc = GL.GetUniformLocation(Handle, name);
			GL.Uniform4(loc, val1, val2, val3, val4);
		}

		public void Uniformi(string name, int val1)
		{
			var loc = GL.GetUniformLocation(Handle, name);
			GL.Uniform1(loc, val1);
		}

		public void Uniformi(string name, int val1, int val2)
		{
			var loc = GL.GetUniformLocation(Handle, name);
			GL.Uniform2(loc, val1, val2);
		}

		public void Uniformi(string name, int val1, int val2, int val3)
		{
			var loc = GL.GetUniformLocation(Handle, name);
			GL.Uniform3(loc, val1, val2, val3);
		}

		public void Uniformi(string name, int val1, int val2, int val3, int val4)
		{
			var loc = GL.GetUniformLocation(Handle, name);
			GL.Uniform4(loc, val1, val2, val3, val4);
		}

		public void UniformMatrix(string name, Matrix4d matrix)
		{
			var loc = GL.GetUniformLocation(Handle, name);
			GL.UniformMatrix4(loc, false, ref matrix);
		}
	}
}

