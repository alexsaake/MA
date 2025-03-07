﻿#version 430

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
    float dt = 0.25;

    uint id = gl_GlobalInvocationID.x;
    myHeightMapSideLength = uint(sqrt(gridPoints.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridPoint gridPoint = gridPoints[id];

    float fromPosX = x - gridPoint.VelocityX * dt;
    float fromPosY = y - gridPoint.VelocityY * dt;

    // integer coordinates
    int x0 = int(fromPosX);
    int y0 = int(fromPosY);
    int x1 = x0 + 1;
    int y1 = y0 + 1;

    // interpolation factors
    float fX = fromPosX - x0;
    float fY = fromPosY - y0;

    // clamp to grid borders
    x0 = int(min(myHeightMapSideLength - 1, max(0, x0)));
    x1 = int(min(myHeightMapSideLength - 1, max(0, x1)));
    y0 = int(min(myHeightMapSideLength - 1, max(0, y0)));
    y1 = int(min(myHeightMapSideLength - 1, max(0, y1)));

    gridPoint.TempSediment = mix(mix(gridPoints[getIndex(x0, y0)].SuspendedSediment, gridPoints[getIndex(x1, y0)].SuspendedSediment, fX), mix(gridPoints[getIndex(x0, y1)].SuspendedSediment, gridPoints[getIndex(x1, y1)].SuspendedSediment, fX), fY);
    
    gridPoints[id] = gridPoint;
}