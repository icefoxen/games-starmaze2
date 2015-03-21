#version 330

in vec2 position;
in vec4 color;
in vec2 texcoord;
uniform vec4 atlasCoords;
uniform sampler2D tex;

smooth out vec4 theColor;
smooth out vec2 theTexcoord;

uniform mat4 projection;

void main()
{
    gl_Position = projection * vec4(position, 1, 1);
    theTexcoord = (texcoord * atlasCoords.zw) + atlasCoords.xy;
    theColor = color;
}