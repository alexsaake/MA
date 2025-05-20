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

struct ErosionConfiguration
{
    float SeaLevel;
    float TimeDelta;
	bool IsWaterKeptInBoundaries;
};

layout(std430, binding = 6) readonly restrict buffer erosionConfigurationShaderBuffer
{
    ErosionConfiguration erosionConfiguration;
};

struct GridThermalErosionCell
{
    float BedrockFlowLeft;
    float BedrockFlowRight;
    float BedrockFlowUp;
    float BedrockFlowDown;
    float CoarseSedimentFlowLeft;
    float CoarseSedimentFlowRight;
    float CoarseSedimentFlowUp;
    float CoarseSedimentFlowDown;
    float FineSedimentFlowLeft;
    float FineSedimentFlowRight;
    float FineSedimentFlowUp;
    float FineSedimentFlowDown;
};

layout(std430, binding = 13) buffer gridThermalErosionCellShaderBuffer
{
    GridThermalErosionCell[] gridThermalErosionCells;
};

uint myHeightMapSideLength;
uint myHeightMapLength;

void RemoveFromBedrock(uint index, float sediment)
{
    heightMap[index] = max(heightMap[index] - sediment, 0.0);
}

void RemoveFromCoarseSediment(uint index, float sediment)
{
    uint offsetIndex = index + 1 * myHeightMapLength;
    heightMap[offsetIndex] = max(heightMap[offsetIndex] - sediment, 0.0);
}

void RemoveFromFineSediment(uint index, float sediment)
{
    uint offsetIndex = index + (mapGenerationConfiguration.LayerCount - 1) * myHeightMapLength;
    heightMap[offsetIndex] = max(heightMap[offsetIndex] - sediment, 0.0);
}

void DepositeOnCoarseSediment(uint index, float sediment)
{
    if(mapGenerationConfiguration.LayerCount > 2)
    {
        heightMap[index + 1 * myHeightMapLength] += sediment;
    }
    else
    {
        heightMap[index + (mapGenerationConfiguration.LayerCount - 1) * myHeightMapLength] += sediment;
    }
}

void DepositeOnFineSediment(uint index, float sediment)
{
    heightMap[index + (mapGenerationConfiguration.LayerCount - 1) * myHeightMapLength] += sediment;
}

float totalHeight(uint index)
{
    float height = 0;
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        height += heightMap[index + layer * myHeightMapLength];
    }
    return height;
}

uint GetIndex(uint x, uint y)
{
    return uint((y * myHeightMapSideLength) + x);
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute

void main()
{    
    uint id = gl_GlobalInvocationID.x;
    myHeightMapLength = heightMap.length() / mapGenerationConfiguration.LayerCount;
    if(id >= myHeightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapLength));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[id];
    
    float bedrockFlowIn = gridThermalErosionCells[GetIndex(x - 1, y)].BedrockFlowRight + gridThermalErosionCells[GetIndex(x + 1, y)].BedrockFlowLeft + gridThermalErosionCells[GetIndex(x, y - 1)].BedrockFlowUp + gridThermalErosionCells[GetIndex(x, y + 1)].BedrockFlowDown;
    float bedrockFlowOut = gridThermalErosionCell.BedrockFlowRight + gridThermalErosionCell.BedrockFlowLeft + gridThermalErosionCell.BedrockFlowUp + gridThermalErosionCell.BedrockFlowDown;
    
	float bedrockVolumeDelta = (bedrockFlowIn - bedrockFlowOut) * erosionConfiguration.TimeDelta;
    if(bedrockVolumeDelta < 0)
    {
        RemoveFromBedrock(id, abs(bedrockVolumeDelta));
    }
    else
    {
        DepositeOnCoarseSediment(id, bedrockVolumeDelta);
    }

    float coarseSedimentFlowIn = gridThermalErosionCells[GetIndex(x - 1, y)].CoarseSedimentFlowRight + gridThermalErosionCells[GetIndex(x + 1, y)].CoarseSedimentFlowLeft + gridThermalErosionCells[GetIndex(x, y - 1)].CoarseSedimentFlowUp + gridThermalErosionCells[GetIndex(x, y + 1)].CoarseSedimentFlowDown;
    float coarseSedimentFlowOut = gridThermalErosionCell.CoarseSedimentFlowRight + gridThermalErosionCell.CoarseSedimentFlowLeft + gridThermalErosionCell.CoarseSedimentFlowUp + gridThermalErosionCell.CoarseSedimentFlowDown;
    
	float coarseSedimentVolumeDelta = (coarseSedimentFlowIn - coarseSedimentFlowOut) * erosionConfiguration.TimeDelta;
    if(coarseSedimentVolumeDelta < 0)
    {
        RemoveFromCoarseSediment(id, abs(coarseSedimentVolumeDelta));
    }
    else
    {
        DepositeOnFineSediment(id, coarseSedimentVolumeDelta);
    }

    float fineSedimentFlowIn = gridThermalErosionCells[GetIndex(x - 1, y)].FineSedimentFlowRight + gridThermalErosionCells[GetIndex(x + 1, y)].FineSedimentFlowLeft + gridThermalErosionCells[GetIndex(x, y - 1)].FineSedimentFlowUp + gridThermalErosionCells[GetIndex(x, y + 1)].FineSedimentFlowDown;
    float fineSedimentFlowOut = gridThermalErosionCell.FineSedimentFlowRight + gridThermalErosionCell.FineSedimentFlowLeft + gridThermalErosionCell.FineSedimentFlowUp + gridThermalErosionCell.FineSedimentFlowDown;
    
	float fineSedimentVolumeDelta = (fineSedimentFlowIn - fineSedimentFlowOut) * erosionConfiguration.TimeDelta;
    if(fineSedimentVolumeDelta < 0)
    {
        RemoveFromFineSediment(id, abs(fineSedimentVolumeDelta));
    }
    else
    {
        DepositeOnFineSediment(id, fineSedimentVolumeDelta);
    }
    
    memoryBarrier();
}