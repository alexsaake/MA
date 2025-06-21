#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 1) buffer heatMapShaderBuffer
{
    float[] heatMap;
};

struct HeightMapParameters
{
    uint Seed;
    float Scale;
    uint Octaves;
    float Persistence;
    float Lacunarity;
    int Min;
    int Max;
};

layout(std430, binding = 12) readonly restrict buffer heightMapParametersShaderBuffer
{
    HeightMapParameters heightMapParameters;
};

float inverseLerp(float lower, float upper, float value)
{
    return (value - lower) / (upper - lower);
}

void main()
{
    uint index = gl_GlobalInvocationID.x;
    if(index >= heatMap.length())
    {
        return;
    }

    heatMap[index] = clamp(inverseLerp(float(heightMapParameters.Min) / 100000, float(heightMapParameters.Max) / 100000, heatMap[index]), 0.0, 1.0);
}