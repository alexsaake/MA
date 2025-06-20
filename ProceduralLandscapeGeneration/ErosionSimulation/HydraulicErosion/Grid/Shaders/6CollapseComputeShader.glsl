﻿#version 430

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
    bool AreLayerColorsEnabled;
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

uint myHeightMapSideLength;
uint myHeightMapPlaneSize;

float HeightMapLayerFloorHeight(uint index, uint layer)
{
    if(layer < 1)
    {
        return 0.0;
    }
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

uint GetIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

uint HeightMapLayerOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

uint HeightMapRockTypeOffset(uint rockType)
{
    return rockType * myHeightMapPlaneSize;
}

float HeightMapLayerHeight(uint index, uint layer)
{
    float heightMapLayerHeight = 0.0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        heightMapLayerHeight += heightMap[index + HeightMapRockTypeOffset(rockType) + HeightMapLayerOffset(layer)];
    }
    return heightMapLayerHeight;
}

float TotalHeightMapLayerHeight(uint index, uint layer)
{
    float heightMapLayerFloorHeight = 0.0;
    if(layer > 0)
    {
        heightMapLayerFloorHeight = HeightMapLayerFloorHeight(index, layer);
        if(heightMapLayerFloorHeight == 0)
        {
            return 0.0;
        }
    }
    return heightMapLayerFloorHeight + HeightMapLayerHeight(index, layer);
}

bool IsFloating(uint index, float floorHeight)
{
    if(floorHeight == 0)
    {
        return false;
    }
    uint x = index % myHeightMapSideLength;
    uint y = index / myHeightMapSideLength;

    uint indexLeft = GetIndex(x - 1, y);
    uint indexRight = GetIndex(x + 1, y);
    uint indexDown = GetIndex(x, y - 1);
    uint indexUp = GetIndex(x, y + 1);
    
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        float totalHeightMapLayerHeightLeft = 0.0;
        if(x > 0)
        {
            totalHeightMapLayerHeightLeft = TotalHeightMapLayerHeight(indexLeft, layer);
        }
        else
        {
            totalHeightMapLayerHeightLeft = floorHeight;
        }
        float totalHeightMapLayerHeightRight = 0.0;
        if(x < myHeightMapSideLength - 1)
        {
            totalHeightMapLayerHeightRight = TotalHeightMapLayerHeight(indexRight, layer);
        }
        else
        {
            totalHeightMapLayerHeightRight = floorHeight;
        }
        float totalHeightMapLayerHeightDown = 0.0;
        if(y > 0)
        {
            totalHeightMapLayerHeightDown = TotalHeightMapLayerHeight(indexDown, layer);
        }
        else
        {
            totalHeightMapLayerHeightDown = floorHeight;
        }
        float totalHeightMapLayerHeightUp = 0.0;
        if(y < myHeightMapSideLength - 1)
        {
            totalHeightMapLayerHeightUp = TotalHeightMapLayerHeight(indexUp, layer);
        }
        else
        {
            totalHeightMapLayerHeightUp = floorHeight;
        }
        if(totalHeightMapLayerHeightLeft > floorHeight
            || totalHeightMapLayerHeightRight > floorHeight
            || totalHeightMapLayerHeightDown > floorHeight
            || totalHeightMapLayerHeightUp > floorHeight)
        {
            return false;
        }
    }

    return true;
}

float LayerFloorCollapseThreshold(uint index, uint layer)
{
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        if(heightMap[index + HeightMapRockTypeOffset(rockType) + HeightMapLayerOffset(layer)] > 0)
        {
            return rockTypesConfiguration[rockType].CollapseThreshold;
        }
    }
    return 0.0;
}

void MoveRockToBelowLayer(uint index)
{
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        uint layerOneRockTypeHeightMapIndex = index + HeightMapRockTypeOffset(rockType) + (mapGenerationConfiguration.RockTypeCount + 1) * myHeightMapPlaneSize;
        uint layerZeroRockTypeHeightMapIndex = index + HeightMapRockTypeOffset(rockType);
        if(mapGenerationConfiguration.RockTypeCount > 1
            && rockType == 0)
        {
            //bedrock becomes coarse sediment
            layerZeroRockTypeHeightMapIndex = index + (rockType + 1) * myHeightMapPlaneSize;
        }
        heightMap[layerZeroRockTypeHeightMapIndex] += heightMap[layerOneRockTypeHeightMapIndex];
        heightMap[layerOneRockTypeHeightMapIndex] = 0;
    }
}

uint LayerHydraulicErosionCellsOffset(uint layer)
{
    return layer * myHeightMapPlaneSize;
}

void DepositeSuspendedSediment(uint index, float suspendedSediment)
{
    uint heightMapLayerZeroFineSedimentIndex = index + HeightMapRockTypeOffset(mapGenerationConfiguration.RockTypeCount - 1);
    heightMap[heightMapLayerZeroFineSedimentIndex] += suspendedSediment;
}

void MoveWaterAndSuspendedSedimentToBelowLayer(uint index, uint layer)
{
    if(layer < 1)
    {
        return;
    }
    uint currentLayerGridHydraulicErosionCellIndex = index + LayerHydraulicErosionCellsOffset(layer);
    uint belowLayerGridHydraulicErosionCellIndex = index + LayerHydraulicErosionCellsOffset(layer - 1);

    gridHydraulicErosionCells[belowLayerGridHydraulicErosionCellIndex].WaterHeight = gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].WaterHeight;
    gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].WaterHeight = 0.0;
    DepositeSuspendedSediment(index, gridHydraulicErosionCells[belowLayerGridHydraulicErosionCellIndex].SuspendedSediment);
    gridHydraulicErosionCells[belowLayerGridHydraulicErosionCellIndex].SuspendedSediment = gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].SuspendedSediment;
    gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].SuspendedSediment = 0.0;
}

void SetHeightMapLayerFloorHeight(uint index, uint layer, float value)
{
    if(layer < 1
        || layer >= mapGenerationConfiguration.LayerCount)
    {
        return;
    }
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
        memoryBarrier();
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapPlaneSize));

    float layerZeroHeightMapHeight = HeightMapLayerHeight(index, 0);
    float layerOneFloorHeight = HeightMapLayerFloorHeight(index, 1);
    if(layerOneFloorHeight == 0)
    {
        return;
    }
    if(!IsFloating(index, layerOneFloorHeight)
        && max(layerOneFloorHeight - layerZeroHeightMapHeight, 0.0) < LayerFloorCollapseThreshold(index, 1))
    {
        memoryBarrier();
        return;
    }

    MoveRockToBelowLayer(index);
    MoveWaterAndSuspendedSedimentToBelowLayer(index, 1);
    SetHeightMapLayerFloorHeight(index, 1, 0);
    
    memoryBarrier();
}