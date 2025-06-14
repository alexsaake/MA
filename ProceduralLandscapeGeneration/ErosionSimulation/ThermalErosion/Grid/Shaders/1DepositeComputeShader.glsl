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

uint LayerHeightMapOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

void RemoveFromTop(uint index, uint rockType, float sediment)
{
    float sedimentToRemove = sediment;
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        uint heightMapOffsetIndex = index + rockType * myHeightMapPlaneSize + LayerHeightMapOffset(layer);
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
        
        float sedimentFlowRight = 0.0;
        if(x > 0)
        {
            sedimentFlowRight = gridThermalErosionCells[GetIndex(x - 1, y) + gridThermalErosionCellsIndexOffset].SedimentFlowRight;
        }
        float sedimentFlowLeft  = 0.0;
		if(x < myHeightMapSideLength - 1)
        {
            sedimentFlowLeft = gridThermalErosionCells[GetIndex(x + 1, y) + gridThermalErosionCellsIndexOffset].SedimentFlowLeft;
        }
        float sedimentFlowUp = 0.0;
        if(y > 0)
        {
            sedimentFlowUp = gridThermalErosionCells[GetIndex(x, y - 1) + gridThermalErosionCellsIndexOffset].SedimentFlowUp;
        }
        float sedimentFlowDown  = 0.0;
		if(y < myHeightMapSideLength - 1)
        {
            sedimentFlowDown = gridThermalErosionCells[GetIndex(x, y + 1) + gridThermalErosionCellsIndexOffset].SedimentFlowDown;
        }
        float flowIn = sedimentFlowRight + sedimentFlowLeft + sedimentFlowUp + sedimentFlowDown;
        float flowOut = gridThermalErosionCell.SedimentFlowRight + gridThermalErosionCell.SedimentFlowLeft + gridThermalErosionCell.SedimentFlowUp + gridThermalErosionCell.SedimentFlowDown;
    
	    float volumeDelta = (flowIn - flowOut) * erosionConfiguration.TimeDelta;
        if(volumeDelta < 0)
        {
            RemoveFromTop(index, rockType, abs(volumeDelta));
        }
        else
        {
            //collapse bedrock to coarse sediment
            if(mapGenerationConfiguration.RockTypeCount > 1
                && rockType == 0)
            {
                DepositeOn(index, rockType + 1, volumeDelta);                
            }
            else
            {
                DepositeOn(index, rockType, volumeDelta);
            }
        }
    }

    memoryBarrier();
}