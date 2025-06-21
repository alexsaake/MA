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

uint myHeightMapPlaneSize;

uint HeightMapLayerOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

void main()
{
    uint index = gl_GlobalInvocationID.x;
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= myHeightMapPlaneSize)
    {
        return;
    }
    uint heightMapSideLength = uint(sqrt(myHeightMapPlaneSize));

    uint x = index % heightMapSideLength;
    uint y = index / heightMapSideLength;
    
    float bedrockLayerOneHeight = 0.0;
    if((y >= heightMapSideLength / 8 * 3
        && x < heightMapSideLength / 2)
        || (y >= heightMapSideLength / 4
        && y < heightMapSideLength / 8 * 3
        && x < heightMapSideLength / 4 * 3))
    {
        bedrockLayerOneHeight = 1.0;
    }
    if(mapGenerationConfiguration.LayerCount > 1
        && mapGenerationConfiguration.RockTypeCount > 1
        && y >= heightMapSideLength / 4
        && y < heightMapSideLength / 8 * 3
        && x >= heightMapSideLength / 2
        && x < heightMapSideLength / 8 * 5)
    {
        uint rockType = 1;
        uint layer = 1;
        bedrockLayerOneHeight = 0.0;
        float coarseRockTypeLayerOneHeight = 0.5;
        heightMap[index + rockType * myHeightMapPlaneSize] = coarseRockTypeLayerOneHeight;
        
        float bedrockLayerTwoHeight = 0.5;
        float layerTwoFloorHeight = 0.5;
        heightMap[index + HeightMapLayerOffset(layer)] = bedrockLayerTwoHeight;
        heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize] = layerTwoFloorHeight;
    }

    heightMap[index] = bedrockLayerOneHeight;
}