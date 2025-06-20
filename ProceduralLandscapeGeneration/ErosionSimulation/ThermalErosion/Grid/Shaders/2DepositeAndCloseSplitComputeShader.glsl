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
    bool AreLayerColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

struct ErosionConfiguration
{
    float DeltaTime;
	bool IsWaterKeptInBoundaries;
};

layout(std430, binding = 6) readonly restrict buffer erosionConfigurationShaderBuffer
{
    ErosionConfiguration erosionConfiguration;
};

struct GridThermalErosionCell
{
    float RockTypeFlowLeft;
    float RockTypeFlowRight;
    float RockTypeFlowUp;
    float RockTypeFlowDown;
};

layout(std430, binding = 13) buffer gridThermalErosionCellShaderBuffer
{
    GridThermalErosionCell[] gridThermalErosionCells;
};

uint myHeightMapSideLength;
uint myHeightMapPlaneSize;

uint GetIndex(uint x, uint y)
{
    return uint((y * myHeightMapSideLength) + x);
}

uint HeightMapLayerOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

uint HeightMapRockTypeOffset(uint rockType)
{
    return rockType * myHeightMapPlaneSize;
}

void RemoveFrom(uint index, uint layer, uint rockType, float sediment)
{
    uint heightMapOffsetIndex = index + HeightMapRockTypeOffset(rockType) + HeightMapLayerOffset(layer);
    heightMap[heightMapOffsetIndex] -= sediment;
}

void DepositeOn(uint index, uint layer, uint rockType, float sediment)
{
    uint heightMapOffsetIndex = index + HeightMapRockTypeOffset(rockType) + HeightMapLayerOffset(layer);
    heightMap[heightMapOffsetIndex] += sediment;
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

float HeightMapLayerFloorHeight(uint index, uint layer)
{
    if(layer < 1
        || layer >= mapGenerationConfiguration.LayerCount)
    {
        return 0.0;
    }
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
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

float HeightMapLayerRockTypeHeight(uint index, uint layer, int stopRockType)
{
    float heightMapLayerRockTypeHeight = 0.0;
    for(int rockType = 0; rockType <= stopRockType; rockType++)
    {
        heightMapLayerRockTypeHeight += heightMap[index + HeightMapRockTypeOffset(uint(rockType)) + HeightMapLayerOffset(layer)];
    }
    return heightMapLayerRockTypeHeight;
}

float TotalHeightMapLayerRockTypeHeight(uint index, uint layer, int stopRockType)
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
    return heightMapLayerFloorHeight + HeightMapLayerRockTypeHeight(index, layer, stopRockType);
}

float LayerSplitSize(uint index, uint layer)
{
    float aboveHeightMapLayerFloorHeight = HeightMapLayerFloorHeight(index, layer + 1);
    if(layer == 0
        && aboveHeightMapLayerFloorHeight > 0.0)
    {
        float totalHeightMapLayerHeight = TotalHeightMapLayerHeight(index, layer);
        return aboveHeightMapLayerFloorHeight - totalHeightMapLayerHeight;
    }
    return 100.0;
}

void SetHeightMapLayerFloorHeight(uint index, uint layer, float value)
{
    heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize] = value;
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute

void main()
{    
    uint index = gl_GlobalInvocationID.x;
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= myHeightMapPlaneSize)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapPlaneSize));

    uint x = index % myHeightMapSideLength;
    uint y = index / myHeightMapSideLength;
    
    uint indexLeft = GetIndex(x - 1, y);
    uint indexRight = GetIndex(x + 1, y);
    uint indexDown = GetIndex(x, y - 1);
    uint indexUp = GetIndex(x, y + 1);

    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        if(layer > 0
            && HeightMapLayerFloorHeight(index, layer) == 0)
        {
            continue;
        }
        
        float totalHeightMapLayerHeight = TotalHeightMapLayerHeight(index, layer);
        float aboveHeightMapLayerFloorHeight = HeightMapLayerFloorHeight(index, layer + 1);
        
	    for(int rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
	    {
            float layerRockTypeInflow = 0.0;
            for(uint layer2 = 0; layer2 < mapGenerationConfiguration.LayerCount; layer2++)
            {
                uint gridThermalErosionCellsOffset2 = rockType * myHeightMapPlaneSize + layer2 * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize;
                if(x > 0)
                {
                    if(TotalHeightMapLayerRockTypeHeight(indexLeft, layer2, rockType) > totalHeightMapLayerHeight
                        && (aboveHeightMapLayerFloorHeight == 0
                            || TotalHeightMapLayerRockTypeHeight(indexLeft, layer2, rockType - 1) < aboveHeightMapLayerFloorHeight))
                    {
                        layerRockTypeInflow += gridThermalErosionCells[indexLeft + gridThermalErosionCellsOffset2].RockTypeFlowRight;
                    }
                }
                if(x < myHeightMapSideLength - 1)
                {
                    if(TotalHeightMapLayerRockTypeHeight(indexRight, layer2, rockType) > totalHeightMapLayerHeight
                        && (aboveHeightMapLayerFloorHeight == 0
                            || TotalHeightMapLayerRockTypeHeight(indexRight, layer2, rockType - 1) < aboveHeightMapLayerFloorHeight))
                    {
                        layerRockTypeInflow += gridThermalErosionCells[indexRight + gridThermalErosionCellsOffset2].RockTypeFlowLeft;
                    }
                }
                if(y > 0)
                {
                    if(TotalHeightMapLayerRockTypeHeight(indexDown, layer2, rockType) > totalHeightMapLayerHeight
                        && (aboveHeightMapLayerFloorHeight == 0
                            || TotalHeightMapLayerRockTypeHeight(indexDown, layer2, rockType - 1) < aboveHeightMapLayerFloorHeight))
                    {
                        layerRockTypeInflow += gridThermalErosionCells[indexDown + gridThermalErosionCellsOffset2].RockTypeFlowUp;
                    }
                }
                if(y < myHeightMapSideLength - 1)
                {
                    if(TotalHeightMapLayerRockTypeHeight(indexUp, layer2, rockType) > totalHeightMapLayerHeight
                        && (aboveHeightMapLayerFloorHeight == 0
                            || TotalHeightMapLayerRockTypeHeight(indexUp, layer2, rockType - 1) < aboveHeightMapLayerFloorHeight))
                    {
                        layerRockTypeInflow += gridThermalErosionCells[indexUp + gridThermalErosionCellsOffset2].RockTypeFlowDown;
                    }
                }
            }
            
            uint gridThermalErosionCellsOffset = rockType * myHeightMapPlaneSize + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize;
            GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[index + gridThermalErosionCellsOffset];
            float layerRockTypeOutflow = gridThermalErosionCell.RockTypeFlowRight + gridThermalErosionCell.RockTypeFlowLeft + gridThermalErosionCell.RockTypeFlowUp + gridThermalErosionCell.RockTypeFlowDown;
	        float volumeDelta = (layerRockTypeInflow - layerRockTypeOutflow) * erosionConfiguration.DeltaTime;
            if(volumeDelta < 0)
            {
                RemoveFrom(index, layer, uint(rockType), abs(volumeDelta));
            }
            else
            {
                //collapse bedrock to coarse sediment
                if(mapGenerationConfiguration.RockTypeCount > 1
                    && rockType == 0)
                {
                    DepositeOn(index, layer, uint(rockType) + 1, volumeDelta);                
                }
                else
                {
                    DepositeOn(index, layer, uint(rockType), volumeDelta);
                }
            }
        }

        float layerSplitSize = LayerSplitSize(index, layer);
        uint gridHydraulicErosionCellsIndexOffset = layer * myHeightMapPlaneSize;
        GridHydraulicErosionCell gridHydraulicErosionCell = gridHydraulicErosionCells[index + gridHydraulicErosionCellsIndexOffset];

        if(layerSplitSize <= gridHydraulicErosionCell.SuspendedSediment)
        {
            DepositeOn(index, layer, mapGenerationConfiguration.RockTypeCount - 1, gridHydraulicErosionCell.SuspendedSediment);
            SetHeightMapLayerFloorHeight(index, layer, 0.0);
            gridHydraulicErosionCell.WaterHeight = 0.0;
            gridHydraulicErosionCell.SuspendedSediment = 0.0;
            gridHydraulicErosionCells[index + gridHydraulicErosionCellsIndexOffset] = gridHydraulicErosionCell;
        }
    }

    memoryBarrier();
}