#version 120
// Vertex shader

uniform vec4 vertexDiff;
uniform int facing;

void main(void) {
   gl_Position = gl_ModelViewProjectionMatrix * ((gl_Vertex * vec4(facing, 1, 1, 1)) + vertexDiff);   
   gl_FrontColor = gl_Color;
   gl_TexCoord[0] = gl_MultiTexCoord0;
}
