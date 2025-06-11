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

struct ErosionConfiguration
{
    float TimeDelta;
	bool IsWaterKeptInBoundaries;
};

layout(std430, binding = 6) readonly restrict buffer erosionConfigurationShaderBuffer
{
    ErosionConfiguration erosionConfiguration;
};

struct GridThermalErosionCell
{
    float SedimentFlowLeft;
    float SedimentFlowRight;
    float SedimentFlowUp;
    float SedimentFlowDown;
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

void RemoveFromTop(uint index, uint rockType, float sediment)
{
    float sedimentToRemove = sediment;
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        uint heightMapOffsetIndex = index + rockType * myHeightMapPlaneSize + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
        float height = heightMap[heightMapOffsetIndex];
        if(sedimentToRemove <= height)
        {
            heightMap[heightMapOffsetIndex] -= sedimentToRemove;
            return;
        }
        else if(sedimentToRemove > 0)
        {
            heightMap[heightMapOffsetIndex] = 0.0;
            sedimentToRemove = max(sedimentToRemove - height, 0.0);
        }
    }
}

void DepositeOn(uint index, uint rockType, float sediment)
{
    heightMap[index + rockType * myHeightMapPlaneSize] += sediment;
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
    
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
		uint gridThermalErosionCellsIndexOffset = rockType * myHeightMapPlaneSize;
        GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[index + gridThermalErosionCellsIndexOffset];
    
        float flowIn = gridThermalErosionCells[GetIndex(x - 1, y) + gridThermalErosionCellsIndexOffset].SedimentFlowRight + gridThermalErosionCells[GetIndex(x + 1, y) + gridThermalErosionCellsIndexOffset].SedimentFlowLeft + gridThermalErosionCells[GetIndex(x, y - 1) + gridThermalErosionCellsIndexOffset].SedimentFlowUp + gridThermalErosionCells[GetIndex(x, y + 1) + gridThermalErosionCellsIndexOffset].SedimentFlowDown;
        float flowOut = gridThermalErosionCell.SedimentFlowRight + gridThermalErosionCell.SedimentFlowLeft + gridThermalErosionCell.SedimentFlowUp + gridThermalErosionCell.SedimentFlowDown;
    
	    float volumeDelta = (flowIn - flowOut) * erosionConfiguration.TimeDelta;
        if(volumeDelta < 0)
        {
            RemoveFromTop(index, rockType, abs(volumeDelta));
        }
        else
        {
            if(rockType == mapGenerationConfiguration.RockTypeCount - 1)
            {
                DepositeOn(index, rockType, volumeDelta);                
            }
            else
            {
                DepositeOn(index, rockType + 1, volumeDelta);
            }
        }
    }

    memoryBarrier();
}