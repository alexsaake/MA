#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
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
    uint id = gl_GlobalInvocationID.x;
    
    uint sideLength = uint(sqrt(heightMap.length()));
    uint mapSize = sideLength * sideLength;
    if (id >= mapSize) return;

    heightMap[id] = inverseLerp(float(heightMapParameters.Min) / 100000, float(heightMapParameters.Max) / 100000, heightMap[id]);
}