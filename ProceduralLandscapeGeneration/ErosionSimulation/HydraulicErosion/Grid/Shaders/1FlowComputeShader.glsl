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
    uint gridHydraulicErosionCellsLength = gridHydraulicErosionCells.length();
    if(id >= gridHydraulicErosionCellsLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(gridHydraulicErosionCellsLength));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridHydraulicErosionCell gridHydraulicErosionCell  = gridHydraulicErosionCells[id];
    
    float totalHeight = heightMap[id] + gridHydraulicErosionCell.WaterHeight;

    if(x > 0)
    {
        float totalHeightLeft = heightMap[getIndex(x - 1, y)] + gridHydraulicErosionCells[getIndex(x - 1, y)].WaterHeight;
        gridHydraulicErosionCell.FlowLeft = max(gridHydraulicErosionCell.FlowLeft + (totalHeight - totalHeightLeft) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
    }
    else
    {
        gridHydraulicErosionCell.FlowLeft = 0.0;
    }

    if(x < myHeightMapSideLength - 1)
    {
        float totalHeightRight = heightMap[getIndex(x + 1, y)] + gridHydraulicErosionCells[getIndex(x + 1, y)].WaterHeight;
        gridHydraulicErosionCell.FlowRight = max(gridHydraulicErosionCell.FlowRight + (totalHeight - totalHeightRight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
    }
    else
    {
        gridHydraulicErosionCell.FlowRight = 0.0;
    }

    if(y > 0)
    {
        float totalHeightBottom = heightMap[getIndex(x, y - 1)] + gridHydraulicErosionCells[getIndex(x, y - 1)].WaterHeight;
        gridHydraulicErosionCell.FlowBottom = max(gridHydraulicErosionCell.FlowBottom + (totalHeight - totalHeightBottom) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
    }
    else
    {
        gridHydraulicErosionCell.FlowBottom = 0.0;
    }

    if(y < myHeightMapSideLength - 1)
    {
        float totalHeightTop = heightMap[getIndex(x, y + 1)] + gridHydraulicErosionCells[getIndex(x, y + 1)].WaterHeight;
        gridHydraulicErosionCell.FlowTop = max(gridHydraulicErosionCell.FlowTop + (totalHeight - totalHeightTop) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
    }
    else
    {
        gridHydraulicErosionCell.FlowTop = 0.0;
    }

    float totalOutflow = gridHydraulicErosionCell.FlowLeft + gridHydraulicErosionCell.FlowRight + gridHydraulicErosionCell.FlowBottom + gridHydraulicErosionCell.FlowTop;
    float scale = min(gridHydraulicErosionCell.WaterHeight / totalOutflow * (1.0 - gridErosionConfiguration.Dampening), 1.0);
        
    gridHydraulicErosionCell.FlowLeft *= scale;
    gridHydraulicErosionCell.FlowRight *= scale;
    gridHydraulicErosionCell.FlowBottom *= scale;
    gridHydraulicErosionCell.FlowTop *= scale;

    gridHydraulicErosionCells[id] = gridHydraulicErosionCell;
    
    memoryBarrier();
}