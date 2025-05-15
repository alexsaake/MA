#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct GridHydraulicErosionCell
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

layout(std430, binding = 4) buffer gridHydraulicErosionCellShaderBuffer
{
    GridHydraulicErosionCell[] gridHydraulicErosionCells;
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
    float Gravity;
    float Dampening;
    float MaximalErosionDepth;
    float SuspensionRate;
    float DepositionRate;
    float EvaporationRate;
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
    uint gridHydraulicErosionCellsLength = gridHydraulicErosionCells.length();
    if(id > gridHydraulicErosionCellsLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(gridHydraulicErosionCellsLength));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridHydraulicErosionCell gridHydraulicErosionCell  = gridHydraulicErosionCells[id];

    float flowIn = gridHydraulicErosionCells[getIndex(x - 1, y)].FlowRight + gridHydraulicErosionCells[getIndex(x + 1, y)].FlowLeft + gridHydraulicErosionCells[getIndex(x, y - 1)].FlowTop + gridHydraulicErosionCells[getIndex(x, y + 1)].FlowBottom;
    float flowOut = gridHydraulicErosionCell.FlowRight + gridHydraulicErosionCell.FlowLeft + gridHydraulicErosionCell.FlowTop + gridHydraulicErosionCell.FlowBottom;

	float volumeDelta = (flowIn - flowOut) * erosionConfiguration.TimeDelta;

	gridHydraulicErosionCell.WaterHeight = max(gridHydraulicErosionCell.WaterHeight + volumeDelta, 0.0);

    if(gridHydraulicErosionCell.WaterHeight > 0.0)
    {
        gridHydraulicErosionCell.Velocity = vec2(clamp(0.5 * (gridHydraulicErosionCells[getIndex(x - 1, y)].FlowRight - gridHydraulicErosionCell.FlowLeft - gridHydraulicErosionCells[getIndex(x + 1, y)].FlowLeft + gridHydraulicErosionCell.FlowRight) * mapGenerationConfiguration.HeightMultiplier, -1.0, 1.0),
                                  clamp(0.5 * (gridHydraulicErosionCells[getIndex(x, y - 1)].FlowTop - gridHydraulicErosionCell.FlowBottom - gridHydraulicErosionCells[getIndex(x, y + 1)].FlowBottom + gridHydraulicErosionCell.FlowTop) * mapGenerationConfiguration.HeightMultiplier, -1.0, 1.0));
    }
    else
    {
        gridHydraulicErosionCell.Velocity = vec2(0);
    }
    
    gridHydraulicErosionCells[id] = gridHydraulicErosionCell;

    memoryBarrier();
}