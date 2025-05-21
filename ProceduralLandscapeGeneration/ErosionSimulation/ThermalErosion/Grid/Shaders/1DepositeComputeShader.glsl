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
uint myHeightMapLength;

void RemoveFrom(uint index, uint rockType, float sediment)
{
    uint offsetIndex = index + rockType * myHeightMapLength;
    heightMap[offsetIndex] = max(heightMap[offsetIndex] - sediment, 0.0);
}

void DepositeOn(uint index, uint rockType, float sediment)
{
    heightMap[index + rockType * myHeightMapLength] += sediment;
}

uint GetIndex(uint x, uint y)
{
    return uint((y * myHeightMapSideLength) + x);
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute

void main()
{    
    uint index = gl_GlobalInvocationID.x;
    myHeightMapLength = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= myHeightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapLength));

    uint x = index % myHeightMapSideLength;
    uint y = index / myHeightMapSideLength;
    
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        uint indexOffset = rockType * myHeightMapLength;
        GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[index + indexOffset];
    
        float flowIn = gridThermalErosionCells[GetIndex(x - 1, y) + indexOffset].SedimentFlowRight + gridThermalErosionCells[GetIndex(x + 1, y) + indexOffset].SedimentFlowLeft + gridThermalErosionCells[GetIndex(x, y - 1) + indexOffset].SedimentFlowUp + gridThermalErosionCells[GetIndex(x, y + 1) + indexOffset].SedimentFlowDown;
        float flowOut = gridThermalErosionCell.SedimentFlowRight + gridThermalErosionCell.SedimentFlowLeft + gridThermalErosionCell.SedimentFlowUp + gridThermalErosionCell.SedimentFlowDown;
    
	    float volumeDelta = (flowIn - flowOut) * erosionConfiguration.TimeDelta;
        if(volumeDelta < 0)
        {
            RemoveFrom(index, rockType, abs(volumeDelta));
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