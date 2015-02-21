#version 330

smooth in vec4 theColor;
smooth in vec2 theTexcoord;
uniform sampler2D texture;

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
		colorAccumulator += texture2D(texture, theTexcoord + vec2(offsetAmount, 0));
	}
	colorAccumulator /= convolutionSize;

	outputColor = vec4(colorAccumulator.rgb, 1);
	//outputColor = vec4(0, 1, 1, 1);


    //outputColor = vec4(texture2D(texture, theTexcoord).rgb, 1);
    //outputColor = outputColor.bgra;
}