#version 330

smooth in vec4 theColor;
smooth in vec2 theTexcoord;
uniform sampler2D tex;

out vec4 outputColor;


#define FXAA_REDUCE_MIN (1.0/128.0)
#define FXAA_REDUCE_MUL (1.0/8.0)
#define FXAA_SPAN_MAX 8.0
vec2 resolution = vec2(1024,1024);

// Shamelessly stolen from https://www.opengl.org/discussion_boards/showthread.php/184192-GLSL-FXAA-rendering-off-screen
// See http://developer.download.nvidia.com/assets/gamedev/files/sdk/11/FXAA_WhitePaper.pdf for algorithm.
void main()
{
    vec2 inverse_resolution=vec2(1.0/resolution.x,1.0/resolution.y);
    vec3 rgbNW = texture(tex, theTexcoord.xy + (vec2(-1.0,-1.0)) * inverse_resolution).xyz;
    vec3 rgbNE = texture(tex, theTexcoord.xy + (vec2(1.0,-1.0)) * inverse_resolution).xyz;
    vec3 rgbSW = texture(tex, theTexcoord.xy + (vec2(-1.0,1.0)) * inverse_resolution).xyz;
    vec3 rgbSE = texture(tex, theTexcoord.xy + (vec2(1.0,1.0)) * inverse_resolution).xyz;
    vec3 rgbM  = texture(tex,  theTexcoord.xy).xyz;
    vec3 luma = vec3(0.299, 0.587, 0.114);
    float lumaNW = dot(rgbNW, luma);
    float lumaNE = dot(rgbNE, luma);
    float lumaSW = dot(rgbSW, luma);
    float lumaSE = dot(rgbSE, luma);
    float lumaM  = dot(rgbM,  luma);
    float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
    float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE))); 
    vec2 dir;
    dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
    dir.y =  ((lumaNW + lumaSW) - (lumaNE + lumaSE));
    float dirReduce = max((lumaNW + lumaNE + lumaSW + lumaSE) * (0.25 * FXAA_REDUCE_MUL),FXAA_REDUCE_MIN);
    float rcpDirMin = 1.0/(min(abs(dir.x), abs(dir.y)) + dirReduce);
    dir = min(vec2( FXAA_SPAN_MAX,  FXAA_SPAN_MAX),max(vec2(-FXAA_SPAN_MAX, -FXAA_SPAN_MAX),dir * rcpDirMin)) * inverse_resolution;
    vec3 rgbA = 0.5 * (texture(tex,   theTexcoord.xy   + dir * (1.0/3.0 - 0.5)).xyz + texture(tex,   theTexcoord.xy   + dir * (2.0/3.0 - 0.5)).xyz);
    vec3 rgbB = rgbA * 0.5 + 0.25 * (texture(tex,  theTexcoord.xy   + dir *  - 0.5).xyz + texture(tex,  theTexcoord.xy   + dir * 0.5).xyz);
    float lumaB = dot(rgbB, luma);
    if((lumaB < lumaMin) || (lumaB > lumaMax)) {
       outputColor = vec4(rgbA,1.0);
    } else {
       outputColor = vec4(rgbB,1.0);
    }
}