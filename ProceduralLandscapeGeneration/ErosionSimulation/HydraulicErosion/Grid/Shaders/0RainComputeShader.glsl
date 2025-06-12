#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

struct GridHydraulicErosionCell
{
    float WaterHeight;

    float WaterFlowLeft;
    float WaterFlowRight;
    float WaterFlowUp;
    float WaterFlowDown;

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
    float TimeDelta;
	bool IsWaterKeptInBoundaries;
};

layout(std430, binding = 6) readonly restrict buffer erosionConfigurationShaderBuffer
{
    ErosionConfiguration erosionConfiguration;
};

struct GridHydraulicErosionConfiguration
{
    float WaterIncrease;
    float Gravity;
    float Dampening;
    float MaximalErosionHeight;
    float MaximalErosionDepth;
    float SedimentCapacity;
    float VerticalSuspensionRate;
    float HorizontalSuspensionRate;
    float DepositionRate;
    float EvaporationRate;
};

layout(std430, binding = 9) buffer gridHydraulicErosionConfigurationShaderBuffer
{
    GridHydraulicErosionConfiguration gridHydraulicErosionConfiguration;
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

    GridHydraulicErosionCell gridHydraulicErosionCell = gridHydraulicErosionCells[index];

    gridHydraulicErosionCell.WaterHeight += gridHydraulicErosionConfiguration.WaterIncrease * erosionConfiguration.TimeDelta;

    gridHydraulicErosionCells[index] = gridHydraulicErosionCell;
    
    memoryBarrier();
}