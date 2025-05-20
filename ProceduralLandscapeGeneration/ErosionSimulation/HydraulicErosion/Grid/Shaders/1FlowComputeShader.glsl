#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

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

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    uint LayerCount;
    bool AreTerrainColorsEnabled;
    bool ArePlateTectonicsPlateColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

struct ErosionConfiguration
{
    float SeaLevel;
    float TimeDelta;
	bool IsWaterKeptInBoundaries;
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

uint myHeightMapSideLength;
uint myHeightMapLength;

float TotalHeight(uint index)
{
    float height = 0;
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        height += heightMap[index + layer * myHeightMapLength];
    }
    return height;
}

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
//adding sediment flow
//https://github.com/Clocktown/CUDA-3D-Hydraulic-Erosion-Simulation-with-Layered-Stacks/blob/main/core/geo/device/transport.cu

void main()
{
    uint id = gl_GlobalInvocationID.x;
    myHeightMapLength = heightMap.length() / mapGenerationConfiguration.LayerCount;
    if(id >= myHeightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapLength));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridHydraulicErosionCell gridHydraulicErosionCell  = gridHydraulicErosionCells[id];
    
    float height = TotalHeight(id);
    if(height < erosionConfiguration.SeaLevel)
    {
        gridHydraulicErosionCell.WaterHeight = erosionConfiguration.SeaLevel - height;
    }
    float totalHeight = (height + gridHydraulicErosionCell.WaterHeight) * mapGenerationConfiguration.HeightMultiplier;
    float outOfBoundsHeight = totalHeight - 0.2;
    if(x > 0)
    {
        uint leftIndex = getIndex(x - 1, y);
        float totalHeightLeft = (TotalHeight(leftIndex) + gridHydraulicErosionCells[leftIndex].WaterHeight) * mapGenerationConfiguration.HeightMultiplier;
        gridHydraulicErosionCell.FlowLeft = max(gridHydraulicErosionCell.FlowLeft + (totalHeight - totalHeightLeft) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        gridHydraulicErosionCell.SedimentFlowLeft = max(gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightLeft) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
    }
    else
    {
        if(erosionConfiguration.IsWaterKeptInBoundaries)
        {
            gridHydraulicErosionCell.FlowLeft = 0.0;
            gridHydraulicErosionCell.SedimentFlowLeft = 0.0;
        }
        else
        {
            gridHydraulicErosionCell.FlowLeft = max(gridHydraulicErosionCell.FlowLeft + (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            gridHydraulicErosionCell.SedimentFlowLeft = max(gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        }
    }

    if(x < myHeightMapSideLength - 1)
    {
        uint rightIndex = getIndex(x + 1, y);
        float totalHeightRight = (TotalHeight(rightIndex) + gridHydraulicErosionCells[rightIndex].WaterHeight) * mapGenerationConfiguration.HeightMultiplier;
        gridHydraulicErosionCell.FlowRight = max(gridHydraulicErosionCell.FlowRight + (totalHeight - totalHeightRight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        gridHydraulicErosionCell.SedimentFlowRight = max(gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightRight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
    }
    else
    {
        if(erosionConfiguration.IsWaterKeptInBoundaries)
        {
            gridHydraulicErosionCell.FlowRight = 0.0;
            gridHydraulicErosionCell.SedimentFlowRight = 0.0;
        }
        else
        {
            gridHydraulicErosionCell.FlowRight = max(gridHydraulicErosionCell.FlowRight + (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            gridHydraulicErosionCell.SedimentFlowRight = max(gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        }
    }

    if(y > 0)
    {
        uint downIndex = getIndex(x, y - 1);
        float totalHeightDown = (TotalHeight(downIndex) + gridHydraulicErosionCells[downIndex].WaterHeight) * mapGenerationConfiguration.HeightMultiplier;
        gridHydraulicErosionCell.FlowDown = max(gridHydraulicErosionCell.FlowDown + (totalHeight - totalHeightDown) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        gridHydraulicErosionCell.SedimentFlowDown = max(gridHydraulicErosionCell.SedimentFlowDown + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightDown) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
    }
    else
    {
        if(erosionConfiguration.IsWaterKeptInBoundaries)
        {
            gridHydraulicErosionCell.FlowDown = 0.0;
            gridHydraulicErosionCell.SedimentFlowDown = 0.0;
        }
        else
        {
            gridHydraulicErosionCell.FlowDown = max(gridHydraulicErosionCell.FlowDown + (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            gridHydraulicErosionCell.SedimentFlowDown = max(gridHydraulicErosionCell.SedimentFlowDown + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        }
    }

    if(y < myHeightMapSideLength - 1)
    {
        uint upIndex = getIndex(x, y + 1);
        float totalHeightUp = (TotalHeight(upIndex) + gridHydraulicErosionCells[upIndex].WaterHeight) * mapGenerationConfiguration.HeightMultiplier;
        gridHydraulicErosionCell.FlowUp = max(gridHydraulicErosionCell.FlowUp + (totalHeight - totalHeightUp) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        gridHydraulicErosionCell.SedimentFlowUp = max(gridHydraulicErosionCell.SedimentFlowUp + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightUp) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
    }
    else
    {
        if(erosionConfiguration.IsWaterKeptInBoundaries)
        {
            gridHydraulicErosionCell.FlowUp = 0.0;
            gridHydraulicErosionCell.SedimentFlowUp = 0.0;
        }
        else
        {
            gridHydraulicErosionCell.FlowUp = max(gridHydraulicErosionCell.FlowUp + (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            gridHydraulicErosionCell.SedimentFlowUp = max(gridHydraulicErosionCell.SedimentFlowUp + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        }
    }

    float totalFlow = gridHydraulicErosionCell.FlowLeft + gridHydraulicErosionCell.FlowRight + gridHydraulicErosionCell.FlowDown + gridHydraulicErosionCell.FlowUp;
    float scale = min(gridHydraulicErosionCell.WaterHeight * mapGenerationConfiguration.HeightMultiplier / totalFlow * erosionConfiguration.TimeDelta * (1.0 - gridErosionConfiguration.Dampening), 1.0);        
    gridHydraulicErosionCell.FlowLeft *= scale;
    gridHydraulicErosionCell.FlowRight *= scale;
    gridHydraulicErosionCell.FlowDown *= scale;
    gridHydraulicErosionCell.FlowUp *= scale;
    
    float totalSedimentFlow = gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SedimentFlowDown + gridHydraulicErosionCell.SedimentFlowUp;
    float sedimentScale = min(gridHydraulicErosionCell.SuspendedSediment * mapGenerationConfiguration.HeightMultiplier / totalSedimentFlow * erosionConfiguration.TimeDelta * (1.0 - gridErosionConfiguration.Dampening), 1.0);
    gridHydraulicErosionCell.SedimentFlowLeft *= sedimentScale;
    gridHydraulicErosionCell.SedimentFlowRight *= sedimentScale;
    gridHydraulicErosionCell.SedimentFlowDown *= sedimentScale;
    gridHydraulicErosionCell.SedimentFlowUp *= sedimentScale;

    gridHydraulicErosionCells[id] = gridHydraulicErosionCell;
    
    memoryBarrier();
}