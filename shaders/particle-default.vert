#version 330

in vec4 position;
in vec4 color;

uniform vec2 offset;
uniform vec4 colorOffset;

smooth out vec4 theColor;

uniform mat4 projection;

void main()
{
    gl_Position = (projection * position) + vec4(offset, 0, 0);
    theColor = vec4(colorOffset.r, colorOffset.g, colorOffset.b, color.a);
}