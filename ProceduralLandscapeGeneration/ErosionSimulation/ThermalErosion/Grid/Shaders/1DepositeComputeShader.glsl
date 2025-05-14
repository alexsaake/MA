#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct ErosionConfiguration
{
    float SeaLevel;
    float TimeDelta;
};

layout(std430, binding = 6) readonly restrict buffer erosionConfigurationShaderBuffer
{
    ErosionConfiguration erosionConfiguration;
};

struct GridThermalErosionCell
{
    float FlowLeft;
    float FlowRight;
    float FlowTop;
    float FlowBottom;
};

layout(std430, binding = 13) buffer gridThermalErosionCellShaderBuffer
{
    GridThermalErosionCell[] gridThermalErosionCells;
};

uint myHeightMapSideLength;

uint getIndex(uint x, uint y)
{
    return uint((y * myHeightMapSideLength) + x);
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute

void main()
{    
    uint id = gl_GlobalInvocationID.x;
    if(id > heightMap.length())
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(gridThermalErosionCells.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[id];

    float flowIn = gridThermalErosionCells[getIndex(x - 1, y)].FlowRight + gridThermalErosionCells[getIndex(x + 1, y)].FlowLeft + gridThermalErosionCells[getIndex(x, y - 1)].FlowTop + gridThermalErosionCells[getIndex(x, y + 1)].FlowBottom;
    float flowOut = gridThermalErosionCell.FlowRight + gridThermalErosionCell.FlowLeft + gridThermalErosionCell.FlowTop + gridThermalErosionCell.FlowBottom;

	float volumeDelta = (flowIn - flowOut) * erosionConfiguration.TimeDelta;

	heightMap[id] = max(heightMap[id] + volumeDelta, 0.0);
    
    memoryBarrier();
}