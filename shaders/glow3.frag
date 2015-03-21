#version 330

smooth in vec4 theColor;
smooth in vec2 theTexcoord;
uniform sampler2D tex;
uniform sampler2D glowTex;

out vec4 outputColor;

void main()
{
	vec4 baseColor = texture(tex, theTexcoord);
	vec4 glowColor = texture(glowTex, theTexcoord);
    outputColor = vec4((baseColor + glowColor).rgb, 1);
    //outputColor = vec4(texture(tex, theTexcoord).rgb, 1);
    //outputColor = outputColor.bgra;
}