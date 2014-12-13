#version 120
// Vertex shader
// Smoothly interpolates from the object color to a different color based on
// a uniform parameter.


uniform vec4 vertexDiff;
uniform int facing;

uniform float amount;
uniform vec4 colorTo;

void main(void) {
   //gl_Position = gl_ProjectionMatrix * gl_ModelViewMatrix * gl_Vertex;
   // Technically equivalent to above but with a facing and scale offset.
   gl_Position = gl_ModelViewProjectionMatrix * ((gl_Vertex * vec4(facing, 1, 1, 1)) + vertexDiff);

   vec4 fromColor = gl_Color * amount;
   vec4 toColor = colorTo * (1.0f - amount);
   // XXX: 
   // This is a little bit of a hack.
   // Ideally, toColor would really be specified per-vertex,
   // but for now we take the vertex's original alpha as canonical.
   toColor.a = fromColor.a;
   
   gl_FrontColor = fromColor + toColor;
   gl_TexCoord[0] = gl_MultiTexCoord0;
}
