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

layout(std430, binding = 6) readonly restrict buffer erosionConfigurationShaderBuffer
{
    ErosionConfiguration erosionConfiguration;
};

struct GridHydraulicErosionConfiguration
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
uint myHeightMapPlaneSize;

float TotalHeight(uint index, uint layer)
{
    float height = 0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapPlaneSize + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize];
    }
    return height;
}

void main()
{    
    uint id = gl_GlobalInvocationID.x;
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
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
    
    uint gridHydraulicErosionCellsIndexOffset = 0;
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        if(TotalHeight(index, uint(layer)) > 0)
        {
            gridHydraulicErosionCellsIndexOffset = layer * myHeightMapPlaneSize;
            break;
        }
    }

    GridHydraulicErosionCell gridHydraulicErosionCell = gridHydraulicErosionCells[index + gridHydraulicErosionCellsIndexOffset];

    gridHydraulicErosionCell.WaterHeight += gridHydraulicErosionConfiguration.WaterIncrease * erosionConfiguration.TimeDelta;

    gridHydraulicErosionCells[index + gridHydraulicErosionCellsIndexOffset] = gridHydraulicErosionCell;
    
    memoryBarrier();
}