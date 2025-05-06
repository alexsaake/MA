#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

struct HeightMapParameters
{
    uint seed;
    float scale;
    uint octaves;
    float persistence;
    float lacunarity;
    int min;
    int max;
};

layout(std430, binding = 1) readonly restrict buffer heightMapParametersBuffer
{
    HeightMapParameters parameters;
};

layout(std430, binding = 2) buffer heightMapShaderBuffer
{
    float[] heightMap;
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

    heightMap[id] = inverseLerp(float(parameters.min) / 100000, float(parameters.max) / 100000, heightMap[id]);
}