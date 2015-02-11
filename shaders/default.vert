#version 330

in vec2 position;
in vec4 color;

smooth out vec4 theColor;

uniform mat4 projection;

void main()
{
    gl_Position = projection * vec4(position, 0, 1);
    theColor = color;
}