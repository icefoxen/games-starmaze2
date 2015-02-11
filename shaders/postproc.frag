#version 330

smooth in vec4 theColor;
smooth in vec2 theTexcoord;
uniform sampler2D texture;

out vec4 outputColor;

void main()
{
    outputColor = vec4(texture2D(texture, theTexcoord).rgb, 1);
}