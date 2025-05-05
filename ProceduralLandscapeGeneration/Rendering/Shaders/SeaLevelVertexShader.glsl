#version 430

struct Configuration
{
    float HeightMultiplier;
    float SeaLevel;
    float IsColorEnabled;
};

layout(std430, binding = 2) readonly restrict buffer configurationShaderBuffer
{
    Configuration configuration;
};

in vec3 vertexPosition;
in vec4 vertexColor;

uniform mat4 mvp;

out vec4 fragColor;

void main()
{
    fragColor = vertexColor;
    gl_Position = mvp * vec4(vertexPosition.xy, configuration.SeaLevel * configuration.HeightMultiplier, 1.0);
}