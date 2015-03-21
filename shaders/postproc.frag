#version 330

smooth in vec4 theColor;
smooth in vec2 theTexcoord;
uniform sampler2D tex;

out vec4 outputColor;

void main()
{
    outputColor = vec4(texture(tex, theTexcoord).rgb, 1);
    //outputColor = outputColor.bgra;
}