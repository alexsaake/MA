#version 430

layout (local_size_x = 16, local_size_y = 16, local_size_z = 1) in;

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

float GetFlowRight(uint x, uint y)
{
    if (x < 0 || x > myHeightMapSideLength - 1)
    {
        return 0.0;
    }
    return gridPoints[getIndex(x, y)].FlowRight;
}

float GetFlowLeft(uint x, uint y)
{
    if (x < 0 || x > myHeightMapSideLength - 1)
    {
        return 0.0;
    }
    return gridPoints[getIndex(x, y)].FlowLeft;
}

float GetFlowBottom(uint x, uint y)
{
    if (y < 0 || y > myHeightMapSideLength - 1)
    {
        return 0.0;
    }
    return gridPoints[getIndex(x, y)].FlowBottom;
}

float GetFlowTop(uint x, uint y)
{
    if (y < 0 || y > myHeightMapSideLength - 1)
    {
        return 0.0;
    }
    return gridPoints[getIndex(x, y)].FlowTop;
}

float Kdmax = 10.0;

float lmax(float waterHeight)
{
    if(waterHeight <= 0)
    {
        return 0;
    }
    if(waterHeight >= Kdmax)
    {
        return 1;
    }
    return 1 - (Kdmax - waterHeight) / Kdmax;
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute

void main()
{
    float dt = 1.0;
    float dx = 1.0;
    float dy = 1.0;

    uint id = gl_GlobalInvocationID.x;
    myHeightMapSideLength = uint(sqrt(gridPoints.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridPoint gridPoint = gridPoints[id];

	float waterHeightBefore = gridPoint.WaterHeight;
    float flowIn = gridPoints[getIndex(x - 1, y)].FlowRight + gridPoints[getIndex(x + 1, y)].FlowLeft + gridPoints[getIndex(x, y - 1)].FlowTop + gridPoints[getIndex(x, y + 1)].FlowBottom;
    float flowOut = gridPoint.FlowRight + gridPoint.FlowLeft + gridPoint.FlowTop + gridPoint.FlowBottom;

	float volumeDelta = flowIn - flowOut;

	gridPoint.WaterHeight += dt * volumeDelta / (dx * dy);

    gridPoint.VelocityX = 0.5 * (gridPoints[getIndex(x - 1, y)].FlowRight - gridPoint.FlowLeft + gridPoints[getIndex(x + 1, y)].FlowLeft - gridPoint.FlowRight);
    gridPoint.VelocityY = 0.5 * (gridPoints[getIndex(x, y - 1)].FlowTop - gridPoint.FlowBottom + gridPoints[getIndex(x, y + 1)].FlowBottom - gridPoint.FlowTop);
		/// _PipeLength * 0.5 * (waterHeightBefore + WATER_HEIGHT(state));

    gridPoints[id] = gridPoint;
}