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
    float Hardness;

    float FlowLeft;
    float FlowRight;
    float FlowTop;
    float FlowBottom;

    float ThermalLeft;
    float ThermalRight;
    float ThermalTop;
    float ThermalBottom;
    
    vec2 Velocity;
};

layout(std430, binding = 4) buffer gridPointsShaderBuffer
{
    GridPoint[] gridPoints;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    bool IsColorEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

struct GridErosionConfiguration
{
    float WaterIncrease;
    float TimeDelta;
    float Gravity;
    float Friction;
    float MaximalErosionDepth;
    float SedimentCapacity;
    float SuspensionRate;
    float DepositionRate;
    float SedimentSofteningRate;
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

    float flowIn = gridPoints[getIndex(x - 1, y)].FlowRight + gridPoints[getIndex(x + 1, y)].FlowLeft + gridPoints[getIndex(x, y - 1)].FlowTop + gridPoints[getIndex(x, y + 1)].FlowBottom;
    float flowOut = gridPoint.FlowRight + gridPoint.FlowLeft + gridPoint.FlowTop + gridPoint.FlowBottom;

	float volumeDelta = gridErosionConfiguration.TimeDelta * (flowIn - flowOut) / mapGenerationConfiguration.HeightMultiplier;

	gridPoint.WaterHeight = max(0.0, gridPoint.WaterHeight + volumeDelta);

    if(gridPoint.WaterHeight > 0.0)
    {
        gridPoint.Velocity = vec2(0.5 * (gridPoints[getIndex(x - 1, y)].FlowRight - gridPoint.FlowLeft - gridPoints[getIndex(x + 1, y)].FlowLeft + gridPoint.FlowRight), 0.5 * (gridPoints[getIndex(x, y - 1)].FlowTop - gridPoint.FlowBottom - gridPoints[getIndex(x, y + 1)].FlowBottom + gridPoint.FlowTop));
    }
    else
    {
        gridPoint.Velocity = vec2(0);
    }
    
    gridPoints[id] = gridPoint;

    memoryBarrier();
}