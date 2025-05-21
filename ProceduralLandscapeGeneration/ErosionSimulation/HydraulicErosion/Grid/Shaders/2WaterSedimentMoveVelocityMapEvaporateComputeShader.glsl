#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

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

    vec2 WaterVelocity;
};

layout(std430, binding = 4) buffer gridHydraulicErosionCellShaderBuffer
{
    GridHydraulicErosionCell[] gridHydraulicErosionCells;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    uint RockTypeCount;
    uint LayerCount;
    float SeaLevel;
    bool AreTerrainColorsEnabled;
    bool ArePlateTectonicsPlateColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
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

uint GetIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute
//https://lisyarus.github.io/blog/posts/simulating-water-over-terrain.html
//https://github.com/karhu/terrain-erosion/blob/master/Simulation/FluidSimulation.cpp
//fixing
//https://github.com/Clocktown/CUDA-3D-Hydraulic-Erosion-Simulation-with-Layered-Stacks/blob/main/core/geo/device/transport.cu

void main()
{
    uint id = gl_GlobalInvocationID.x;
    uint heightMapLength = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(id >= heightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(heightMapLength));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridHydraulicErosionCell gridHydraulicErosionCell  = gridHydraulicErosionCells[id];

    float waterFlowIn = gridHydraulicErosionCells[GetIndex(x - 1, y)].WaterFlowRight + gridHydraulicErosionCells[GetIndex(x + 1, y)].WaterFlowLeft + gridHydraulicErosionCells[GetIndex(x, y - 1)].WaterFlowUp + gridHydraulicErosionCells[GetIndex(x, y + 1)].WaterFlowDown;
    float waterFlowOut = gridHydraulicErosionCell.WaterFlowRight + gridHydraulicErosionCell.WaterFlowLeft + gridHydraulicErosionCell.WaterFlowUp + gridHydraulicErosionCell.WaterFlowDown;
	float waterVolumeDelta = (waterFlowIn - waterFlowOut) * erosionConfiguration.TimeDelta / mapGenerationConfiguration.HeightMultiplier;
	gridHydraulicErosionCell.WaterHeight = max(gridHydraulicErosionCell.WaterHeight + waterVolumeDelta, 0.0);
    
    float sedimentFlowIn = gridHydraulicErosionCells[GetIndex(x - 1, y)].SedimentFlowRight + gridHydraulicErosionCells[GetIndex(x + 1, y)].SedimentFlowLeft + gridHydraulicErosionCells[GetIndex(x, y - 1)].SedimentFlowUp + gridHydraulicErosionCells[GetIndex(x, y + 1)].SedimentFlowDown;
    float sedimentFlowOut = gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SedimentFlowUp + gridHydraulicErosionCell.SedimentFlowDown;
	float sedimentVolumeDelta = (sedimentFlowIn - sedimentFlowOut) * erosionConfiguration.TimeDelta / mapGenerationConfiguration.HeightMultiplier;
	gridHydraulicErosionCell.SuspendedSediment = max(gridHydraulicErosionCell.SuspendedSediment + sedimentVolumeDelta, 0.0);

    if(gridHydraulicErosionCell.WaterHeight > 0.0
        && x > 0 && x < myHeightMapSideLength - 1
        && y > 0 && y < myHeightMapSideLength - 1)
    {
        gridHydraulicErosionCell.WaterVelocity = 0.5 * vec2(((gridHydraulicErosionCell.WaterFlowRight - gridHydraulicErosionCells[GetIndex(x + 1, y)].WaterFlowLeft) - (gridHydraulicErosionCell.WaterFlowLeft - gridHydraulicErosionCells[GetIndex(x - 1, y)].WaterFlowRight)),
                                                       ((gridHydraulicErosionCell.WaterFlowUp - gridHydraulicErosionCells[GetIndex(x, y + 1)].WaterFlowDown) - (gridHydraulicErosionCell.WaterFlowDown - gridHydraulicErosionCells[GetIndex(x, y - 1)].WaterFlowUp)));
    }
    else
    {
        gridHydraulicErosionCell.WaterVelocity = vec2(0);
    }
    
    gridHydraulicErosionCell.WaterHeight = max(0.0, gridHydraulicErosionCell.WaterHeight * (1.0 - gridErosionConfiguration.EvaporationRate * erosionConfiguration.TimeDelta));
    
    gridHydraulicErosionCells[id] = gridHydraulicErosionCell;

    memoryBarrier();
}