# From http://swiftcoder.wordpress.com/2008/12/19/simple-glsl-wrapper-for-pyglet/
#
# Copyright Tristam Macdonald 2008.
#
# Distributed under the Boost Software License, Version 1.0
# (see http://www.boost.org/LICENSE_1_0.txt)
#
# Modified by Simon Heath, 2014
# Changes: Let you push and pop a stack of shaders
# Though it's not thread-safe.
 
from pyglet.gl import *

# That's opengl 2.1
# WHICH I GUESS WE'RE USING CAUSE I CAN'T FIND DOCS ON ANYTHING ELSE
# AND WE GOTTA AIM AT THE LOWEST COMMON DENOMINATOR ANYWAY
# BECAUSE COMPUTERS SUCK AND I HATE THEM.
# Also my laptop's graphics drivers target OpenGL 3.0,
# and the only docs I can can find on the opengl website
# are for 3.2 and up.
vprog = '''#version 120
// Vertex shader


uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
 
uniform vec4 vertexDiff;
uniform int facing;
 
void main(void) {
   //gl_Position = gl_ProjectionMatrix * gl_ModelViewMatrix * gl_Vertex;
   // Technically equivalent to above but fewer fp multiplications is better.
   gl_Position = gl_ModelViewProjectionMatrix * ((gl_Vertex * vec4(facing, 1, 1, 1)) + vertexDiff);
   gl_FrontColor = gl_Color;
}

'''

fprog = '''#version 120
// Fragment shader

uniform sampler2D tex;

uniform vec4 colorDiff;

void main() {
   //gl_FragColor = colormod;
   gl_FragColor = gl_Color + colorDiff;
   //gl_FragColor = vec4(1,0,1,1);
   //gl_FragColor = texture2D(tex, gl_TexCoord[0].st);
}
'''

# XXX: Not thread-safe!
_root_shader = None

class Shader:
    # vert, frag and geom take arrays of source strings
    # the arrays will be concattenated into one string by OpenGL
    def __init__(self, vert = [], frag = [], geom = []):
        # create the program handle
        self.handle = glCreateProgram()
        # we are not linked yet
        self.linked = False
 
        # create the vertex shader
        self.createShader(vert, GL_VERTEX_SHADER)
        # create the fragment shader
        self.createShader(frag, GL_FRAGMENT_SHADER)
        # the geometry shader will be the same, once pyglet supports the extension
        # self.createShader(frag, GL_GEOMETRY_SHADER_EXT)
 
        # attempt to link the program
        self.link()

        self.parent = None

    def __enter__(self):
        self.bind()

    def __exit__(self, type, val, backtrace):
        self.unbind()
 
    def createShader(self, strings, type):
        count = len(strings)
        # if we have no source code, ignore this shader
        if count < 1:
            return
 
        # create the shader handle
        shader = glCreateShader(type)
 
        # convert the source strings into a ctypes pointer-to-char array, and upload them
        # this is deep, dark, dangerous black magick - don't try stuff like this at home!
        src = (c_char_p * count)(*strings)
        glShaderSource(shader, count, cast(pointer(src), POINTER(POINTER(c_char))), None)
 
        # compile the shader
        glCompileShader(shader)
 
        temp = c_int(0)
        # retrieve the compile status
        glGetShaderiv(shader, GL_COMPILE_STATUS, byref(temp))
 
        # if compilation failed, print the log
        if not temp:
            # retrieve the log length
            glGetShaderiv(shader, GL_INFO_LOG_LENGTH, byref(temp))
            # create a buffer for the log
            buffer = create_string_buffer(temp.value)
            # retrieve the log text
            glGetShaderInfoLog(shader, temp, None, buffer)
            # print the log to the console
            print buffer.value
        else:
            # all is well, so attach the shader to the program
            glAttachShader(self.handle, shader);
 
    def link(self):
        # link the program
        glLinkProgram(self.handle)
 
        temp = c_int(0)
        # retrieve the link status
        glGetProgramiv(self.handle, GL_LINK_STATUS, byref(temp))
 
        # if linking failed, print the log
        if not temp:
            #   retrieve the log length
            glGetProgramiv(self.handle, GL_INFO_LOG_LENGTH, byref(temp))
            # create a buffer for the log
            buffer = create_string_buffer(temp.value)
            # retrieve the log text
            glGetProgramInfoLog(self.handle, temp, None, buffer)
            # print the log to the console
            print buffer.value
        else:
            # all is well, so we are linked
            self.linked = True
 
    def bind(self):
        """Bind the program, pushing itself onto the stack of programs."""
        global _root_shader
        self._parent = _root_shader
        _root_shader = self
        glUseProgram(self.handle)

    def unbind(self):
        """Unbind the program, restoring the previous one (or none, if
it was at the top of the stack.)"""
        global _root_shader
        _root_shader = self._parent
        if _root_shader is None:
            glUseProgram(0)
        else:
            glUseProgram(_root_shader.handle)
 
    # upload a floating point uniform
    # this program must be currently bound
    def uniformf(self, name, *vals):
        # check there are 1-4 values
        if len(vals) in range(1, 5):
            # select the correct function
            func = { 1 : glUniform1f,
                2 : glUniform2f,
                3 : glUniform3f,
                4 : glUniform4f
                # retrieve the uniform location, and set
            }[len(vals)]
            shader = glGetUniformLocation(self.handle, name)
            func(shader, *vals)
 
    # upload an integer uniform
    # this program must be currently bound
    def uniformi(self, name, *vals):
        # check there are 1-4 values
        if len(vals) in range(1, 5):
            # select the correct function
            { 1 : glUniform1i,
                2 : glUniform2i,
                3 : glUniform3i,
                4 : glUniform4i
                # retrieve the uniform location, and set
            }[len(vals)](glGetUniformLocation(self.handle, name), *vals)
 
    # upload a uniform matrix
    # works with matrices stored as lists,
    # as well as euclid matrices
    def uniform_matrixf(self, name, mat):
        # obtian the uniform location
        loc = glGetUniformLocation(self.Handle, name)
        # uplaod the 4x4 floating point matrix
        glUniformMatrix4fv(loc, 1, False, (c_float * 16)(*mat))


DEFAULT_SHADER = Shader([vprog], [fprog])
