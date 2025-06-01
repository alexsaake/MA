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

    vec2 Velocity;
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

struct ErosionConfiguration
{
    float TimeDelta;
	bool IsWaterKeptInBoundaries;
};

layout(std430, binding = 6) readonly restrict buffer erosionConfigurationShaderBuffer
{
    ErosionConfiguration erosionConfiguration;
};

struct GridHydraulicErosionConfiguration
{
    float WaterIncrease;
    float Gravity;
    float Dampening;
    float MaximalErosionDepth;
    float SedimentCapacity;
    float SuspensionRate;
    float DepositionRate;
    float EvaporationRate;
};

layout(std430, binding = 9) buffer gridHydraulicErosionConfigurationShaderBuffer
{
    GridHydraulicErosionConfiguration gridHydraulicErosionConfiguration;
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

float LayerFloorHeight(uint index, uint layer)
{
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

float TotalHeightMapLayerHeight(uint index, uint layer)
{
    if(layer > 0
        && LayerFloorHeight(index, layer) == 0)
    {
        return 0;
    }
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
        float totalHeightMapBellowLayerHeight = TotalHeightMapLayerHeight(index, layer - 1);
        float layerFloorHeight = LayerFloorHeight(index, layer);
        if(layerFloorHeight == 0
            || layerFloorHeight - totalHeightMapBellowLayerHeight < rockTypesConfiguration[0].CollapseThreshold)
        {
            continue;
        }

        for(int rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
        {
            uint currentLayerRockTypeHeightMapIndex = index + rockType * myHeightMapPlaneSize + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
            uint bellowLayerRockTypeHeightMapIndex = index + rockType * myHeightMapPlaneSize + ((layer - 1) * mapGenerationConfiguration.RockTypeCount + (layer - 1)) * myHeightMapPlaneSize;
            uint currentLayerFloorHeightMapIndex = index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize;
            if(mapGenerationConfiguration.RockTypeCount > 0
                && rockType == 0)
            {
                //bedrock becomes coarse sediment
                bellowLayerRockTypeHeightMapIndex = index + rockType + 1 * myHeightMapPlaneSize + ((layer - 1) * mapGenerationConfiguration.RockTypeCount + (layer - 1)) * myHeightMapPlaneSize;
            }
            heightMap[bellowLayerRockTypeHeightMapIndex] += heightMap[currentLayerRockTypeHeightMapIndex];
            heightMap[currentLayerRockTypeHeightMapIndex] = 0;
            heightMap[currentLayerFloorHeightMapIndex] = 0;
        }
    }
    
    memoryBarrier();
}