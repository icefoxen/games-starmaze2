#version 330

smooth in vec4 theColor;
smooth in vec2 theTexcoord;
uniform sampler2D tex;

out vec4 outputColor;

uniform int texels;
float texelSize = 1.0 / texels;

uniform int convolutionRadius;
int convolutionSize = convolutionRadius * 2 + 1;

void main()
{

	vec4 colorAccumulator = vec4(0);
	// This is just a simple boxcar filter... a gaussian distribution might be nicer.
	for(int i = -convolutionRadius; i <= convolutionRadius; i++) {
		float offsetAmount = texelSize * i;
		colorAccumulator += texture(tex, theTexcoord + vec2(0, offsetAmount));
	}
	colorAccumulator /= convolutionSize;

	float glowAlpha = length(colorAccumulator.rgb);

	outputColor = vec4(colorAccumulator.rgb, glowAlpha);


    //outputColor = vec4(texture(tex, theTexcoord).rgb, 1);
    //outputColor = outputColor.bgra;
}