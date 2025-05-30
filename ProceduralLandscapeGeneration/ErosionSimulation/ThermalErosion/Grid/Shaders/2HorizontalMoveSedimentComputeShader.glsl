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
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

uint myHeightMapPlaneSize;

float BedrockHeightMapLayerHeight(uint index, uint layer)
{
    return heightMap[index + 0 * myHeightMapPlaneSize + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize];
}

float LayerFloorHeight(uint index, uint layer)
{
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

float TotalHeightMapLayerHeight(uint index, uint layer)
{
    float height = 0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapPlaneSize + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize];
    }
    return height;
}

//horizontal flow
//https://github.com/Clocktown/CUDA-3D-Hydraulic-Erosion-Simulation-with-Layered-Stacks/blob/main/core/geo/device/transport.cu

void main()
{
    uint index = gl_GlobalInvocationID.x;
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= myHeightMapPlaneSize)
    {
        return;
    }

    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer > 0; layer--)
    {
        if(BedrockHeightMapLayerHeight(index, layer) > 0
            || (mapGenerationConfiguration.SeaLevel > 0
            && LayerFloorHeight(index, layer) == mapGenerationConfiguration.SeaLevel
            && TotalHeightMapLayerHeight(index, layer - 1) == mapGenerationConfiguration.SeaLevel))
        {
            continue;
        }
        for(int rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
        {
            uint currentLayerRockTypeHeightMapIndex = index + rockType * myHeightMapPlaneSize + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
            uint belowLayerRockTypeHeightMapIndex = index + rockType * myHeightMapPlaneSize + ((layer - 1) * mapGenerationConfiguration.RockTypeCount + (layer - 1)) * myHeightMapPlaneSize;
            heightMap[belowLayerRockTypeHeightMapIndex] += heightMap[currentLayerRockTypeHeightMapIndex];
            heightMap[currentLayerRockTypeHeightMapIndex] = 0;
        }
    }
    
    memoryBarrier();
}