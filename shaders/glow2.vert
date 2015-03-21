#version 330

in vec2 position;
in vec4 color;
in vec2 texcoord;
uniform sampler2D texs;

smooth out vec4 theColor;
smooth out vec2 theTexcoord;

uniform mat4 projection;

void main()
{
    gl_Position = projection * vec4(position, 0, 1);
    //gl_Position = vec4(position, 1, 1);
    theTexcoord = texcoord;
    theColor = color;
}