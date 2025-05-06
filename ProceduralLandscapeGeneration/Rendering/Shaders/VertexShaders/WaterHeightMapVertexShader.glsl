#version 430

layout(std430, binding = 1) readonly restrict buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    float SeaLevel;
    float IsColorEnabled;
};

layout(std430, binding = 2) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

struct GridPoint
{
    float WaterHeight;
    float SuspendedSediment;
    float TempSediment;
    float Hardness;

    float FlowLeft;
    float FlowRight;
    float FlowTop;
    float FlowBottom;

    float ThermalLeft;
    float ThermalRight;
    float ThermalTop;
    float ThermalBottom;

    float VelocityX;
    float VelocityY;
};

layout(std430, binding = 3) buffer gridPointsShaderBuffer
{
    GridPoint[] gridPoints;
};

in vec3 vertexPosition;

uniform mat4 mvp;

out vec4 fragColor;

vec4 waterColor = vec4(0.0, 0.0, 1.0, 0.25);

void main()
{
    uint index = gl_VertexID;

    fragColor = waterColor;
    float zOffset = 0.01;
    gl_Position =  mvp * vec4(vertexPosition.xy, (heightMap[index] - zOffset + gridPoints[index].WaterHeight) * mapGenerationConfiguration.HeightMultiplier, 1.0);
}