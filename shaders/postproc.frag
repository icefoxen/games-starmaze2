

uniform sampler2D fbo_texture;
varying vec2 f_texcoord;
 
void main(void) {
  //gl_FragColor = texture2D(fbo_texture, f_texcoord);
  //gl_FragColor = vec4(1, 1, 0, 1);
  vec4 texColor = texture2D(fbo_texture, gl_TexCoord[0].st);
  gl_FragColor = texColor;
  //gl_FragColor = vec4(1, 1, 0, 1);
}

