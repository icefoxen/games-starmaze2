#version 330

smooth in vec4 theColor;
smooth in vec2 theTexcoord;
uniform sampler2D texture;
uniform sampler2D glowTexture;

out vec4 outputColor;

void main()
{
	vec4 baseColor = texture2D(texture, theTexcoord);
	vec4 glowColor = texture2D(glowTexture, theTexcoord);
    outputColor = vec4((baseColor + glowColor).rgb, 1);
    //outputColor = vec4(texture2D(texture, theTexcoord).rgb, 1);
    //outputColor = outputColor.bgra;
}