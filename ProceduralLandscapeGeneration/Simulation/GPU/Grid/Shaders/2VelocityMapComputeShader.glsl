#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 1) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct GridPoint
{
    float WaterHeight;
    float SuspendedSediment;
    float TempSediment;
    float Hardness;

    float FlowLeft;
    float FlowRight;
    float FlowTop;
    float FlowBottom;

    float ThermalLeft;
    float ThermalRight;
    float ThermalTop;
    float ThermalBottom;

    float VelocityX;
    float VelocityY;
};

layout(std430, binding = 2) buffer gridPointsShaderBuffer
{
    GridPoint[] gridPoints;
};

uint myHeightMapSideLength;

uint getIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute
//https://lisyarus.github.io/blog/posts/simulating-water-over-terrain.html

void main()
{
    float timeDelta = 1.0;
    float cellSizeX = 1.0;
    float cellSizeY = 1.0;
    
    uint id = gl_GlobalInvocationID.x;
    uint heightMapLength = heightMap.length();
    if(id > heightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(gridPoints.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridPoint gridPoint = gridPoints[id];

    float flowIn = gridPoints[getIndex(x - 1, y)].FlowRight + gridPoints[getIndex(x + 1, y)].FlowLeft + gridPoints[getIndex(x, y - 1)].FlowTop + gridPoints[getIndex(x, y + 1)].FlowBottom;
    float flowOut = gridPoint.FlowRight + gridPoint.FlowLeft + gridPoint.FlowTop + gridPoint.FlowBottom;

	float volumeDelta = flowIn - flowOut;

	gridPoint.WaterHeight += timeDelta * volumeDelta / (cellSizeX * cellSizeY);

    gridPoint.VelocityX = 0.5 * (gridPoints[getIndex(x - 1, y)].FlowRight - gridPoint.FlowLeft - gridPoints[getIndex(x + 1, y)].FlowLeft + gridPoint.FlowRight);
    gridPoint.VelocityY = 0.5 * (gridPoints[getIndex(x, y - 1)].FlowTop - gridPoint.FlowBottom - gridPoints[getIndex(x, y + 1)].FlowBottom + gridPoint.FlowTop);

    gridPoints[id] = gridPoint;

    memoryBarrier();
}