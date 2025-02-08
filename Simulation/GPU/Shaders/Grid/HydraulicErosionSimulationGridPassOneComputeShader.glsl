#version 430

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 1) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

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

layout(std430, binding = 2) buffer gridPointsShaderBuffer
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
    uint id = gl_GlobalInvocationID.x;
    uint myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    float A = 0.00005f;
    float g = 9.81f;
    float l = 1.0f;

    float fluxFactor = A * g / l;
    
    uint index = getIndex(x, y);

    float dh;
    float h0 = heightMap[index] + gridPoints[index].WaterHeight;
    float newFlux;

    if(x > 0)
    {
        dh = h0 - heightMap[getIndex(x - 1, y)] + gridPoints[getIndex(x - 1, y)].WaterHeight;
        newFlux = gridPoints[index].FlowLeft + fluxFactor * dh;
        gridPoints[index].FlowLeft = max(0.0f, newFlux);
    }
    else
    {
        gridPoints[index].FlowLeft = 0.0;
    }

    if(x < myHeightMapSideLength - 1)
    {
        dh = h0 - heightMap[getIndex(x + 1, y)] + gridPoints[getIndex(x + 1, y)].WaterHeight;
        newFlux = gridPoints[index].FlowRight + fluxFactor * dh;
        gridPoints[index].FlowRight = max(0.0f, newFlux);
    }
    else
    {
        gridPoints[index].FlowRight = 0.0;
    }

    if(y > 0)
    {
        dh = h0 - heightMap[getIndex(x - 1, y)] + gridPoints[getIndex(x, y - 1)].WaterHeight;
        newFlux = gridPoints[index].FlowBottom + fluxFactor * dh;
        gridPoints[index].FlowBottom = max(0.0f, newFlux);
    }
    else
    {
        gridPoints[index].FlowBottom = 0.0;
    }

    if(y < myHeightMapSideLength - 1)
    {
        dh = h0 - heightMap[getIndex(x + 1, y)] + gridPoints[getIndex(x, y + 1)].WaterHeight;
        newFlux = gridPoints[index].FlowTop + fluxFactor * dh;
        gridPoints[index].FlowTop = max(0.0f, newFlux);
    }
    else
    {
        gridPoints[index].FlowTop = 0.0;
    }

    float sumFlux = gridPoints[index].FlowLeft + gridPoints[index].FlowRight + gridPoints[index].FlowBottom + gridPoints[index].FlowTop;
    if (sumFlux > 0.0f)
    {
        float K = min(1.0f, gridPoints[index].WaterHeight / sumFlux);

        gridPoints[index].FlowLeft *= K;
        gridPoints[index].FlowRight *= K;
        gridPoints[index].FlowBottom *= K;
        gridPoints[index].FlowTop *= K;
    }
}