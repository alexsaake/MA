#version 430

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

struct GridPoint
{
    float WaterHeight;
    float SuspendedSediment;
    float TempSediment;

    float FlowLeft;
    float FlowRight;
    float FlowTop;
    float FlowBottom;

    float VelocityX;
    float VelocityY;
};

layout(std430, binding = 1) buffer gridPointsShaderBuffer
{
    GridPoint[] gridPoints;
};

uint myHeightMapSideLength;

uint getIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

void main()
{
    float Ke = 0.00001;

    uint id = gl_GlobalInvocationID.x;
    myHeightMapSideLength = uint(sqrt(gridPoints.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;

    uint index = getIndex(x, y);
    
    gridPoints[index].SuspendedSediment = gridPoints[index].TempSediment;

    //WaterDecrease
    gridPoints[index].WaterHeight = max(gridPoints[index].WaterHeight - Ke, 0);
}