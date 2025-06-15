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

uint LayerHeightMapOffset(uint layer)
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
    
    float bedrockHeight = 0.8 + 0.2 * ((abs(x - (heightMapSideLength / 2.0)) / heightMapSideLength) * 2);
    float flatBedrockHeight = bedrockHeight;
    if(x > heightMapSideLength / 2 - heightMapSideLength / 20
        && x < heightMapSideLength / 2 + heightMapSideLength / 20)
    {
            bedrockHeight = 0.4 + 0.4 * (((abs(x - (heightMapSideLength / 2.0)) / heightMapSideLength) * 2) + ((heightMapSideLength - y) / float(heightMapSideLength)));
    }
    if(mapGenerationConfiguration.LayerCount > 1)
    {
        float layerOneBedrockHeight = 0.0;
        float layerOneFloorHeight = 0.0;
        if(x > heightMapSideLength / 3 * 1
            && x < heightMapSideLength / 3 * 2
            && y > heightMapSideLength / 3 * 1
            && y < heightMapSideLength / 3 * 2)
        {
            layerOneBedrockHeight = 0.1;
            layerOneFloorHeight = flatBedrockHeight;
        }
        
        uint layer = 1;
        heightMap[index + LayerHeightMapOffset(layer)] = layerOneBedrockHeight;
        heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize] = layerOneFloorHeight;
    }

    heightMap[index] = bedrockHeight;
}