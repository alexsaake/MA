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

void main()
{
    uint index = gl_GlobalInvocationID.x;
    uint heightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= heightMapPlaneSize)
    {
        return;
    }
    uint heightMapSideLength = uint(sqrt(heightMapPlaneSize));

    uint x = index % heightMapSideLength;
    uint y = index / heightMapSideLength;
    
    float bedrockHeight = 0.8 + 0.2 * ((abs(x - (heightMapSideLength / 2.0)) / heightMapSideLength) * 2);
    if(x > heightMapSideLength / 2 - heightMapSideLength / 80
        && x < heightMapSideLength / 2 + heightMapSideLength / 80)
    {
            bedrockHeight = 0.4 + 0.4 * (((abs(x - (heightMapSideLength / 2.0)) / heightMapSideLength) * 2) + ((heightMapSideLength - y) / float(heightMapSideLength)));
    }

    heightMap[index] = bedrockHeight;
}