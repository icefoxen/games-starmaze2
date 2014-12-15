

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

