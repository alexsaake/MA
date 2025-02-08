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

float GetFlowRight(uint x, uint y)
{
    if (x < 0 || x > myHeightMapSideLength - 1)
    {
        return 0.0f;
    }
    return gridPoints[getIndex(x, y)].FlowRight;
}

float GetFlowLeft(uint x, uint y)
{
    if (x < 0 || x > myHeightMapSideLength - 1)
    {
        return 0.0f;
    }
    return gridPoints[getIndex(x, y)].FlowLeft;
}

float GetFlowBottom(uint x, uint y)
{
    if (y < 0 || y > myHeightMapSideLength - 1)
    {
        return 0.0f;
    }
    return gridPoints[getIndex(x, y)].FlowBottom;
}

float GetFlowTop(uint x, uint y)
{
    if (y < 0 || y > myHeightMapSideLength - 1)
    {
        return 0.0f;
    }
    return gridPoints[getIndex(x, y)].FlowTop;
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    myHeightMapSideLength = uint(sqrt(gridPoints.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;

    uint index = getIndex(x, y);

    float inFlow = GetFlowRight(x - 1, y) + GetFlowLeft(x + 1, y) + GetFlowTop(x, y - 1) + GetFlowBottom(x, y + 1);
    float outFlow = GetFlowRight(x, y) + GetFlowLeft(x, y) + GetFlowTop(x, y) + GetFlowBottom(x, y);
    float dV = inFlow - outFlow;
    float oldWater = gridPoints[index].WaterHeight;
    gridPoints[index].WaterHeight += dV;
    gridPoints[index].WaterHeight = max(0.0f, gridPoints[index].WaterHeight);

    float meanWater = 0.5f * (oldWater + gridPoints[index].WaterHeight);

    if (meanWater == 0.0f)
    {
        gridPoints[index].VelocityX = 0.0f;
        gridPoints[index].VelocityY = 0.0f;
    }
    else
    {
        gridPoints[index].VelocityX = 0.5f * (GetFlowRight(x - 1, y) - GetFlowLeft(x, y) - GetFlowLeft(x + 1, y) + GetFlowRight(x, y)) / meanWater;
        gridPoints[index].VelocityY = 0.5f * (GetFlowTop(x, y - 1) - GetFlowBottom(x, y) - GetFlowBottom(x, y + 1) + GetFlowTop(x, y)) / meanWater;
    }
}