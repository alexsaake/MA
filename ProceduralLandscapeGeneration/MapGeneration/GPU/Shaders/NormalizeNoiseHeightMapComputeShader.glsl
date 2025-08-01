#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    uint RockTypeCount;
    uint LayerCount;
    float SeaLevel;
    bool AreTerrainColorsEnabled;
    bool ArePlateTectonicsPlateColorsEnabled;
    bool AreLayerColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
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
    uint heightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= heightMapPlaneSize)
    {
        return;
    }

    heightMap[index] = clamp(inverseLerp(float(heightMapParameters.Min) / 100000, float(heightMapParameters.Max) / 100000, heightMap[index]), 0.0, 1.0);
}