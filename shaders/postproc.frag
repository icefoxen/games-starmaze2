#version 330

smooth in vec4 theColor;
smooth in vec2 theTexcoord;
uniform sampler2D texture;

out vec4 outputColor;

void main()
{
    //outputColor = vec4(1.0, 1.0, 0.0, 1.0);
    outputColor = texture2D(texture, theTexcoord);
}

/*
uniform sampler2D fbo_texture;
varying vec2 f_texcoord;

uniform float offset;
 
void main(void) {

  vec2 texcoord = gl_TexCoord[0].st;
  //texcoord.y += sin(texcoord.x * 3.14159 + offset) / 100;
  vec4 texColor = texture2D(fbo_texture, texcoord);
  gl_FragColor = vec4(1.0, 1.0, 1.0, 1.0);
}
*/