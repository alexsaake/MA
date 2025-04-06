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
    float gravity = 9.81;
    float friction = 1.0;
    
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

    float totalHeight = heightMap[id] + gridPoint.WaterHeight;
    float frictionFactor = 0;//pow(1 - friction, timeDelta);

    if(x > 0)
    {
        float totalHeightLeft = heightMap[getIndex(x - 1, y)] + gridPoints[getIndex(x - 1, y)].WaterHeight;
        gridPoint.FlowLeft = gridPoint.FlowLeft * frictionFactor + (totalHeight - totalHeightLeft) * gravity * timeDelta / cellSizeX;
        gridPoint.FlowLeft = max(0.0, gridPoint.FlowLeft);
    }
    else
    {
        gridPoint.FlowLeft = 0.0;
    }

    if(x < myHeightMapSideLength - 1)
    {
        float totalHeightRight = heightMap[getIndex(x + 1, y)] + gridPoints[getIndex(x + 1, y)].WaterHeight;
        gridPoint.FlowRight = gridPoint.FlowRight * frictionFactor + (totalHeight - totalHeightRight) * gravity * timeDelta / cellSizeX;
        gridPoint.FlowRight = max(0.0, gridPoint.FlowRight);
    }
    else
    {
        gridPoint.FlowRight = 0.0;
    }

    if(y > 0)
    {
        float totalHeightBottom = heightMap[getIndex(x, y - 1)] + gridPoints[getIndex(x, y - 1)].WaterHeight;
        gridPoint.FlowBottom = gridPoint.FlowBottom * frictionFactor + (totalHeight - totalHeightBottom) * gravity * timeDelta / cellSizeY;
        gridPoint.FlowBottom = max(0.0, gridPoint.FlowBottom);
    }
    else
    {
        gridPoint.FlowBottom = 0.0;
    }

    if(y < myHeightMapSideLength - 1)
    {
        float totalHeightTop = heightMap[getIndex(x, y + 1)] + gridPoints[getIndex(x, y + 1)].WaterHeight;
        gridPoint.FlowTop = gridPoint.FlowTop * frictionFactor + (totalHeight - totalHeightTop) * gravity * timeDelta / cellSizeY;
        gridPoint.FlowTop = max(0.0, gridPoint.FlowTop);
    }
    else
    {
        gridPoint.FlowTop = 0.0;
    }

    float totalOutflow = gridPoint.FlowLeft + gridPoint.FlowRight + gridPoint.FlowBottom + gridPoint.FlowTop;
    if (totalOutflow > gridPoint.WaterHeight)
    {
        float scale = min(1.0, gridPoint.WaterHeight * cellSizeX * cellSizeY / (totalOutflow * timeDelta));
        
        gridPoint.FlowLeft *= scale;
        gridPoint.FlowRight *= scale;
        gridPoint.FlowBottom *= scale;
        gridPoint.FlowTop *= scale;
    }

    gridPoints[id] = gridPoint;
}