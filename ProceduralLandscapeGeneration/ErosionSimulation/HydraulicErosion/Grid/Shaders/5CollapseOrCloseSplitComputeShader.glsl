#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct GridHydraulicErosionCell
{
    float WaterHeight;

    float WaterFlowLeft;
    float WaterFlowRight;
    float WaterFlowUp;
    float WaterFlowDown;

    float SuspendedSediment;

    float SedimentFlowLeft;
    float SedimentFlowRight;
    float SedimentFlowUp;
    float SedimentFlowDown;
    
    vec2 WaterVelocity;
};

layout(std430, binding = 4) buffer gridHydraulicErosionCellShaderBuffer
{
    GridHydraulicErosionCell[] gridHydraulicErosionCells;
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

struct RockTypeConfiguration
{
    float Hardness;
    float TangensAngleOfRepose;
    float CollapseThreshold;
};

layout(std430, binding = 18) buffer rockTypesConfigurationShaderBuffer
{
    RockTypeConfiguration[] rockTypesConfiguration;
};

uint myHeightMapPlaneSize;

float HeightMapFloorHeight(uint index, uint layer)
{
    if(layer < 1)
    {
        return 0.0;
    }
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

float LayerHeightMapHeight(uint index, uint layer)
{
    float height = 0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapPlaneSize + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize];
    }
    return height;
}

void SetHeightMapFloorHeight(uint index, uint layer, float value)
{
    heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize] = value;
}

//horizontal flow
//https://github.com/Clocktown/CUDA-3D-Hydraulic-Erosion-Simulation-with-Layered-Stacks/blob/main/core/geo/device/transport.cu

void main()
{
    uint index = gl_GlobalInvocationID.x;
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= myHeightMapPlaneSize
        || mapGenerationConfiguration.LayerCount < 2)
    {
        return;
    }

    float layerZeroHeightMapHeight = LayerHeightMapHeight(index, 0);
    float layerOneFloorHeight = HeightMapFloorHeight(index, 1);
    bool isSplitOpen = layerOneFloorHeight > layerZeroHeightMapHeight;
    if(layerOneFloorHeight == 0
        || (isSplitOpen
            && layerOneFloorHeight - layerZeroHeightMapHeight < rockTypesConfiguration[0].CollapseThreshold))
    {
        return;
    }

    for(int rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        uint layerOneRockTypeHeightMapIndex = index + rockType * myHeightMapPlaneSize + (mapGenerationConfiguration.RockTypeCount + 1) * myHeightMapPlaneSize;
        uint layerZeroRockTypeHeightMapIndex = index + rockType * myHeightMapPlaneSize;
        if(isSplitOpen
            && mapGenerationConfiguration.RockTypeCount > 1
            && rockType == 0)
        {
            //bedrock becomes coarse sediment
            layerZeroRockTypeHeightMapIndex = index + (rockType + 1) * myHeightMapPlaneSize;
        }
        heightMap[layerZeroRockTypeHeightMapIndex] += heightMap[layerOneRockTypeHeightMapIndex];
        heightMap[layerOneRockTypeHeightMapIndex] = 0;
    }
        
    SetHeightMapFloorHeight(index, 1, 0);
    
    memoryBarrier();
}