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

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute
//https://lisyarus.github.io/blog/posts/simulating-water-over-terrain.html
//https://github.com/karhu/terrain-erosion/blob/master/Simulation/FluidSimulation.cpp
//fixing
//https://github.com/Clocktown/CUDA-3D-Hydraulic-Erosion-Simulation-with-Layered-Stacks/blob/main/core/geo/device/transport.cu
uint myHeightMapSideLength;
uint myHeightMapPlaneSize;

uint GetIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

void main()
{
    uint index = gl_GlobalInvocationID.x;
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= myHeightMapPlaneSize)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapPlaneSize));

    uint x = index % myHeightMapSideLength;
    uint y = index / myHeightMapSideLength;
    
    GridHydraulicErosionCell gridHydraulicErosionCell  = gridHydraulicErosionCells[index];
        
    uint indexLeft = GetIndex(x - 1, y);
    uint indexRight = GetIndex(x + 1, y);
    uint indexDown = GetIndex(x, y - 1);
    uint indexUp = GetIndex(x, y + 1);

    float waterFlowIn = gridHydraulicErosionCells[indexLeft].WaterFlowRight + gridHydraulicErosionCells[indexRight].WaterFlowLeft + gridHydraulicErosionCells[indexDown].WaterFlowUp + gridHydraulicErosionCells[indexUp].WaterFlowDown;
    float waterFlowOut = gridHydraulicErosionCell.WaterFlowRight + gridHydraulicErosionCell.WaterFlowLeft + gridHydraulicErosionCell.WaterFlowUp + gridHydraulicErosionCell.WaterFlowDown;
	float waterVolumeDelta = (waterFlowIn - waterFlowOut) * erosionConfiguration.TimeDelta;
	gridHydraulicErosionCell.WaterHeight = max(gridHydraulicErosionCell.WaterHeight + waterVolumeDelta, 0.0);
    
    float sedimentFlowIn = gridHydraulicErosionCells[indexLeft].SedimentFlowRight + gridHydraulicErosionCells[indexRight].SedimentFlowLeft + gridHydraulicErosionCells[indexDown].SedimentFlowUp + gridHydraulicErosionCells[indexUp].SedimentFlowDown;
    float sedimentFlowOut = gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SedimentFlowUp + gridHydraulicErosionCell.SedimentFlowDown;
	float sedimentVolumeDelta = (sedimentFlowIn - sedimentFlowOut) * erosionConfiguration.TimeDelta;
	gridHydraulicErosionCell.SuspendedSediment = max(gridHydraulicErosionCell.SuspendedSediment + sedimentVolumeDelta, 0.0);

    if(gridHydraulicErosionCell.WaterHeight > 0.0
        && x > 0 && x < myHeightMapSideLength - 1
        && y > 0 && y < myHeightMapSideLength - 1)
    {
        gridHydraulicErosionCell.WaterVelocity = 0.5 * vec2(((gridHydraulicErosionCell.WaterFlowRight - gridHydraulicErosionCells[GetIndex(x + 1, y)].WaterFlowLeft) - (gridHydraulicErosionCell.WaterFlowLeft - gridHydraulicErosionCells[GetIndex(x - 1, y)].WaterFlowRight)) * mapGenerationConfiguration.HeightMultiplier,
                                                        ((gridHydraulicErosionCell.WaterFlowUp - gridHydraulicErosionCells[GetIndex(x, y + 1)].WaterFlowDown) - (gridHydraulicErosionCell.WaterFlowDown - gridHydraulicErosionCells[GetIndex(x, y - 1)].WaterFlowUp)) * mapGenerationConfiguration.HeightMultiplier);
    }
    else
    {
        gridHydraulicErosionCell.WaterVelocity = vec2(0);
    }
    gridHydraulicErosionCell.WaterHeight = max(gridHydraulicErosionCell.WaterHeight * (1.0 - gridHydraulicErosionConfiguration.EvaporationRate * erosionConfiguration.TimeDelta), 0.0);
    
    gridHydraulicErosionCells[index] = gridHydraulicErosionCell;

    memoryBarrier();
}