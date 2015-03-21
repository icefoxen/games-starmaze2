#version 330

smooth in vec4 theColor;
smooth in vec2 theTexcoord;

uniform sampler2D tex;

out vec4 outputColor;

void main()
{
    outputColor = texture(tex, theTexcoord);
}