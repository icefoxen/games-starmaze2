#version 120
// Fragment shader

uniform sampler2D tex;

uniform vec4 colorDiff;
uniform float alpha;
void main() {
   vec4 color = vec4(gl_Color.r, gl_Color.g, gl_Color.b, gl_Color.a * alpha);
   vec4 texColor = texture2D(tex, gl_TexCoord[0].st);
   gl_FragColor = color; // + texColor;
   //gl_FragColor = vec4(1, 0, 1, 1);
   //gl_FragColor = colormod;
   //gl_FragColor = gl_Color + colorDiff;
   //gl_FragColor = vec4(1,0,1,1);
   //gl_FragColor = texture2D(tex, gl_TexCoord[0].st);
}
