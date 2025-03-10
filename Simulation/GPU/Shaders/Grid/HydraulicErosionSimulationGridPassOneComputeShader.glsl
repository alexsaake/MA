﻿#version 430

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

void main()
{
    float timeDelta = 0.25;
    float pipeLength = 1.0 / 256;
    float cellSizeX = 1.0 / 256;
    float cellSizeY = 1.0 / 256;
    float pipeArea = 20;
    float gravity = 9.81;

    uint id = gl_GlobalInvocationID.x;
    uint myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;

    float fluxFactor = timeDelta * pipeArea * gravity / pipeLength;
    
    GridPoint gridPoint = gridPoints[id];

    float dh;
    float h0 = heightMap[id] + gridPoint.WaterHeight;
    float newFlux;

    if(x > 0)
    {
        dh = h0 - heightMap[getIndex(x - 1, y)] - gridPoints[getIndex(x - 1, y)].WaterHeight;
        newFlux = gridPoint.FlowLeft + fluxFactor * dh;
        gridPoint.FlowLeft = max(0.0, newFlux);
    }
    else
    {
        gridPoint.FlowLeft = 0.0;
    }

    if(x < myHeightMapSideLength - 1)
    {
        dh = h0 - heightMap[getIndex(x + 1, y)] - gridPoints[getIndex(x + 1, y)].WaterHeight;
        newFlux = gridPoint.FlowRight + fluxFactor * dh;
        gridPoint.FlowRight = max(0.0, newFlux);
    }
    else
    {
        gridPoint.FlowRight = 0.0;
    }

    if(y > 0)
    {
        dh = h0 - heightMap[getIndex(x, y - 1)] - gridPoints[getIndex(x, y - 1)].WaterHeight;
        newFlux = gridPoint.FlowBottom + fluxFactor * dh;
        gridPoint.FlowBottom = max(0.0, newFlux);
    }
    else
    {
        gridPoint.FlowBottom = 0.0;
    }

    if(y < myHeightMapSideLength - 1)
    {
        dh = h0 - heightMap[getIndex(1, y + 1)] - gridPoints[getIndex(x, y + 1)].WaterHeight;
        newFlux = gridPoint.FlowTop + fluxFactor * dh;
        gridPoint.FlowTop = max(0.0, newFlux);
    }
    else
    {
        gridPoint.FlowTop = 0.0;
    }

    float sumFlux = gridPoint.FlowLeft + gridPoint.FlowRight + gridPoint.FlowBottom + gridPoint.FlowTop;
    if (sumFlux > 0.0)
    {
        float K = min(1.0f, gridPoint.WaterHeight * cellSizeX * cellSizeY / (sumFlux * timeDelta));

        gridPoint.FlowLeft *= K;
        gridPoint.FlowRight *= K;
        gridPoint.FlowBottom *= K;
        gridPoint.FlowTop *= K;
    }

    gridPoints[id] = gridPoint;
}