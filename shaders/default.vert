#version 120
// Vertex shader
// That's opengl 2.1
// WHICH I GUESS WE'RE USING CAUSE I CAN'T FIND DOCS ON ANYTHING ELSE
// AND WE GOTTA AIM AT THE LOWEST COMMON DENOMINATOR ANYWAY
// BECAUSE COMPUTERS SUCK AND I HATE THEM.
// Also my laptop's graphics drivers target OpenGL 3.0,
// and the only docs I can can find on the opengl website
// are for 3.2 and up.


uniform vec4 vertexDiff;
uniform int facing;

void main(void) {
   //gl_Position = gl_ProjectionMatrix * gl_ModelViewMatrix * gl_Vertex;
   // Technically equivalent to above but fewer fp multiplications is better.
   gl_Position = gl_ModelViewProjectionMatrix * ((gl_Vertex * vec4(facing, 1, 1, 1)) + vertexDiff);
   gl_Position = gl_Vertex;

   
   gl_FrontColor = gl_Color;
   gl_TexCoord[0] = gl_MultiTexCoord0;
}
