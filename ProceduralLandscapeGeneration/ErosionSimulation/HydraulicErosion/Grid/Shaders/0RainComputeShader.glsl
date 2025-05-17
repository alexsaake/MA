#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

struct GridHydraulicErosionCell
{
    float WaterHeight;
    float FlowLeft;
    float FlowRight;
    float FlowUp;
    float FlowDown;
    float SuspendedSediment;
    float SedimentFlowLeft;
    float SedimentFlowRight;
    float SedimentFlowUp;
    float SedimentFlowDown;
    vec2 Velocity;
};

layout(std430, binding = 4) buffer gridHydraulicErosionCellShaderBuffer
{
    GridHydraulicErosionCell[] gridHydraulicErosionCells;
};

struct ErosionConfiguration
{
    float SeaLevel;
    float TimeDelta;
};

layout(std430, binding = 6) readonly restrict buffer erosionConfigurationShaderBuffer
{
    ErosionConfiguration erosionConfiguration;
};

struct GridErosionConfiguration
{
    float WaterIncrease;
    float Gravity;
    float Dampening;
    float MaximalErosionDepth;
    float SedimentCapacity;
    float SuspensionRate;
    float DepositionRate;
    float EvaporationRate;
};

layout(std430, binding = 9) buffer gridErosionConfigurationShaderBuffer
{
    GridErosionConfiguration gridErosionConfiguration;
};

layout(std430, binding = 11) buffer hydraulicErosionHeightMapIndicesShaderBuffer
{
    int[] hydraulicErosionHeightMapIndices;
};

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute
//https://lisyarus.github.io/blog/posts/simulating-water-over-terrain.html
//https://github.com/karhu/terrain-erosion/blob/master/Simulation/FluidSimulation.cpp

void main()
{    
    uint id = gl_GlobalInvocationID.x;
    if(id >= hydraulicErosionHeightMapIndices.length())
    {
        return;
    }

    int index = hydraulicErosionHeightMapIndices[id];
    if(index < 0)
    {
        return;
    }
    hydraulicErosionHeightMapIndices[id] = -1;

    GridHydraulicErosionCell gridHydraulicErosionCell  = gridHydraulicErosionCells[index];

    gridHydraulicErosionCell.WaterHeight += gridErosionConfiguration.WaterIncrease * erosionConfiguration.TimeDelta;

    gridHydraulicErosionCells[index] = gridHydraulicErosionCell;
    
    memoryBarrier();
}