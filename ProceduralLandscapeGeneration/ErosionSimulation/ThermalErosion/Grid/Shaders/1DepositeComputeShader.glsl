#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
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
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute

void main()
{    
    float timeDelta = 1.0;
    float thermalErosionTimeScale = 1.0;
    
    uint id = gl_GlobalInvocationID.x;
    uint heightMapLength = heightMap.length();
    if(id > heightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(gridThermalErosionCells.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[id];

    float flowIn = gridThermalErosionCells[getIndex(x - 1, y)].FlowRight + gridThermalErosionCells[getIndex(x + 1, y)].FlowLeft + gridThermalErosionCells[getIndex(x, y - 1)].FlowTop + gridThermalErosionCells[getIndex(x, y + 1)].FlowBottom;
    float flowOut = gridThermalErosionCell.FlowRight + gridThermalErosionCell.FlowLeft + gridThermalErosionCell.FlowTop + gridThermalErosionCell.FlowBottom;

	float volumeDelta = flowIn - flowOut;

	heightMap[id] = clamp(heightMap[id] + volumeDelta * timeDelta, 0.0, 1.0);
    
    memoryBarrier();
}