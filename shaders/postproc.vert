#version 330


layout (location = 0) in vec2 position;
layout (location = 1) in vec4 color;
layout (location = 2) in vec2 texcoord;
uniform sampler2D texture;

smooth out vec4 theColor;
smooth out vec2 theTexcoord;

uniform mat4 projection;

void main()
{
    gl_Position = vec4(position, 0, 0);
    theColor = color;
    theTexcoord = texcoord;
}




/*

attribute vec2 v_coord;
uniform sampler2D fbo_texture;
varying vec2 f_texcoord;

void main(void) {
  //gl_Position = vec4(v_coord, 0.0, 1.0);
  //f_texcoord = (v_coord + 1.0) / 2.0;
  //f_texcoord = gl_Position;

   gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
   gl_FrontColor = gl_Color;
   gl_TexCoord[0] = gl_MultiTexCoord0;
}

*/