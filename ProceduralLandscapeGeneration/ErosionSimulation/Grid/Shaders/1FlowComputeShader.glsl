#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
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
    vec2 Velocity;
};

layout(std430, binding = 4) buffer gridPointsShaderBuffer
{
    GridPoint[] gridPoints;
};

struct GridErosionConfiguration
{
    float WaterIncrease;
    float TimeDelta;
    float Gravity;
    float Dampening;
    float MaximalErosionDepth;
    float SuspensionRate;
    float DepositionRate;
    float EvaporationRate;
};

layout(std430, binding = 9) buffer gridErosionConfigurationShaderBuffer
{
    GridErosionConfiguration gridErosionConfiguration;
};

uint myHeightMapSideLength;

uint getIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute
//https://lisyarus.github.io/blog/posts/simulating-water-over-terrain.html
//https://github.com/karhu/terrain-erosion/blob/master/Simulation/FluidSimulation.cpp
//damping
//https://github.com/patiltanma/15618-FinalProject/blob/master/Renderer/Renderer/erosion_kernel.cu

void main()
{    
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

    if(x > 0)
    {
        float totalHeightLeft = heightMap[getIndex(x - 1, y)] + gridPoints[getIndex(x - 1, y)].WaterHeight;
        gridPoint.FlowLeft = max(0.0, gridPoint.FlowLeft + (totalHeight - totalHeightLeft) * gridErosionConfiguration.Gravity * gridErosionConfiguration.TimeDelta);
    }
    else
    {
        gridPoint.FlowLeft = 0.0;
    }

    if(x < myHeightMapSideLength - 1)
    {
        float totalHeightRight = heightMap[getIndex(x + 1, y)] + gridPoints[getIndex(x + 1, y)].WaterHeight;
        gridPoint.FlowRight = max(0.0, gridPoint.FlowRight + (totalHeight - totalHeightRight) * gridErosionConfiguration.Gravity * gridErosionConfiguration.TimeDelta);
    }
    else
    {
        gridPoint.FlowRight = 0.0;
    }

    if(y > 0)
    {
        float totalHeightBottom = heightMap[getIndex(x, y - 1)] + gridPoints[getIndex(x, y - 1)].WaterHeight;
        gridPoint.FlowBottom = max(0.0, gridPoint.FlowBottom + (totalHeight - totalHeightBottom) * gridErosionConfiguration.Gravity * gridErosionConfiguration.TimeDelta);
    }
    else
    {
        gridPoint.FlowBottom = 0.0;
    }

    if(y < myHeightMapSideLength - 1)
    {
        float totalHeightTop = heightMap[getIndex(x, y + 1)] + gridPoints[getIndex(x, y + 1)].WaterHeight;
        gridPoint.FlowTop = max(0.0, gridPoint.FlowTop + (totalHeight - totalHeightTop) * gridErosionConfiguration.Gravity * gridErosionConfiguration.TimeDelta);
    }
    else
    {
        gridPoint.FlowTop = 0.0;
    }

    float totalOutflow = gridPoint.FlowLeft + gridPoint.FlowRight + gridPoint.FlowBottom + gridPoint.FlowTop;
    float scale = min(1.0, gridPoint.WaterHeight / (totalOutflow * gridErosionConfiguration.TimeDelta) * (1.0 - gridErosionConfiguration.Dampening));
        
    gridPoint.FlowLeft *= scale;
    gridPoint.FlowRight *= scale;
    gridPoint.FlowBottom *= scale;
    gridPoint.FlowTop *= scale;

    gridPoints[id] = gridPoint;
    
    memoryBarrier();
}