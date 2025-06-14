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

uint myHeightMapSideLength;
uint myHeightMapPlaneSize;

float LayerHeightMapFloorHeight(uint index, uint layer)
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

uint LayerHeightMapOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

float LayerHeightMapRockTypeHeight(uint index, uint layer)
{
    float layerHeightMapRockTypeHeight = 0.0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        layerHeightMapRockTypeHeight += heightMap[index + rockType * myHeightMapPlaneSize + LayerHeightMapOffset(layer)];
    }
    return layerHeightMapRockTypeHeight;
}

float TotalLayerHeightMapHeight(uint index, uint layer)
{
    float layerHeightMapFloorHeight = 0.0;
    if(layer > 0)
    {
        layerHeightMapFloorHeight = LayerHeightMapFloorHeight(index, layer);
        if(layerHeightMapFloorHeight == 0)
        {
            return 0.0;
        }
    }
    return layerHeightMapFloorHeight + LayerHeightMapRockTypeHeight(index, layer);
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
    
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        if(TotalLayerHeightMapHeight(indexLeft, layer) > floorHeight
            || TotalLayerHeightMapHeight(indexRight, layer) > floorHeight
            || TotalLayerHeightMapHeight(indexDown, layer) > floorHeight
            || TotalLayerHeightMapHeight(indexUp, layer) > floorHeight)
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
        if(heightMap[index + rockType * myHeightMapPlaneSize + LayerHeightMapOffset(layer)] > 0)
        {
            return rockTypesConfiguration[rockType].CollapseThreshold;
        }
    }
    return 0.0;
}

void MoveRockToBelowLayer(uint index, bool isSplitOpen)
{
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
}

uint LayerHydraulicErosionCellsOffset(uint layer)
{
    return layer * myHeightMapPlaneSize;
}

void MoveWaterAndSuspendedSedimentToBelowLayer(uint index, uint layer)
{
    if(layer < 1)
    {
        return;
    }
    uint currentLayerGridHydraulicErosionCellIndex = index + LayerHydraulicErosionCellsOffset(layer);
    uint belowLayerGridHydraulicErosionCellIndex = index + LayerHydraulicErosionCellsOffset(layer - 1);

    gridHydraulicErosionCells[belowLayerGridHydraulicErosionCellIndex].WaterHeight += gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].WaterHeight;
    gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].WaterHeight = 0.0;
    gridHydraulicErosionCells[belowLayerGridHydraulicErosionCellIndex].SuspendedSediment += gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].SuspendedSediment;
    gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].SuspendedSediment = 0.0;
}

void SetLayerHeightMapFloorHeight(uint index, uint layer, float value)
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
        memoryBarrier();
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapPlaneSize));

    float layerZeroHeightMapHeight = LayerHeightMapRockTypeHeight(index, 0);
    float layerOneFloorHeight = LayerHeightMapFloorHeight(index, 1);
    bool isSplitOpen = layerOneFloorHeight > layerZeroHeightMapHeight;
    if(!IsFloating(index, layerOneFloorHeight)
        && isSplitOpen
        && max(layerOneFloorHeight - layerZeroHeightMapHeight, 0.0) < LayerFloorCollapseThreshold(index, 1))
    {
        memoryBarrier();
        return;
    }

    MoveRockToBelowLayer(index, isSplitOpen);
    MoveWaterAndSuspendedSedimentToBelowLayer(index, 1);
    SetLayerHeightMapFloorHeight(index, 1, 0);
    
    memoryBarrier();
}