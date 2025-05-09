#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

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

struct GridErosionConfiguration
{
    float WaterIncrease;
    float TimeDelta;
    float CellSizeX;
    float CellSizeY;
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

layout(std430, binding = 11) buffer heightMapIndicesShaderBuffer
{
    int[] heightMapIndices;
};

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute
//https://lisyarus.github.io/blog/posts/simulating-water-over-terrain.html
//https://github.com/karhu/terrain-erosion/blob/master/Simulation/FluidSimulation.cpp

void main()
{    
    uint id = gl_GlobalInvocationID.x;
    if(id >= heightMapIndices.length())
    {
        return;
    }

    int index = heightMapIndices[id];
    if(index < 0)
    {
        return;
    }
    heightMapIndices[id] = -1;

    GridPoint gridPoint = gridPoints[index];

    gridPoint.WaterHeight += gridErosionConfiguration.WaterIncrease;

    gridPoints[index] = gridPoint;
    
    memoryBarrier();
}