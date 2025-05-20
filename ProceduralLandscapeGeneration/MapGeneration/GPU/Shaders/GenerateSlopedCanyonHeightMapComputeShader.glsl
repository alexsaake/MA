#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    uint LayerCount;
    bool AreTerrainColorsEnabled;
    bool ArePlateTectonicsPlateColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

void main()
{
    uint index = gl_GlobalInvocationID.x;
    uint heightMapLength = heightMap.length() / mapGenerationConfiguration.LayerCount;
    if(index >= heightMapLength)
    {
        return;
    }
    uint heightMapSideLength = uint(sqrt(heightMapLength));

    uint x = index % heightMapSideLength;
    uint y = index / heightMapSideLength;
    
    float bedrockHeight = 0.1 + 0.9 * min(pow((abs(x - (heightMapSideLength / 2.0)) / heightMapSideLength) * 2, 2) + ((heightMapSideLength - y) / float(heightMapSideLength)), 1.0);
    heightMap[index] = bedrockHeight;

    if(mapGenerationConfiguration.LayerCount > 1)
    {
        float sedimentHeight = max(((1.0 - bedrockHeight) - 0.05) * (0.9 + 0.1 * (abs(x - (heightMapSideLength / 2.0)) / heightMapSideLength) * 2), 0.0);
        heightMap[index + 1 * heightMapLength] = sedimentHeight;
    }
}