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

uint myHeightMapSideLength;
uint myHeightMapPlaneSize;

uint GetIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

float HeightMapFloorHeight(uint index, uint layer)
{
    if(layer < 1)
    {
        return 0.0;
    }
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

float TotalHeightMapHeight(uint index)
{
    float heightMapFloorHeight = 0.0;
    float rockTypeHeight = 0.0;
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
		heightMapFloorHeight = 0.0;
        if(layer > 0)
        {
            heightMapFloorHeight = HeightMapFloorHeight(index, layer);
            if(heightMapFloorHeight == 0)
            {
                continue;
            }
        }
        for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
        {
            rockTypeHeight += heightMap[index + rockType * myHeightMapPlaneSize + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize];
        }
        if(rockTypeHeight > 0)
        {
            return heightMapFloorHeight + rockTypeHeight;
        }
    }
    return heightMapFloorHeight + rockTypeHeight;
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
    uint index = gl_GlobalInvocationID.x;
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= myHeightMapPlaneSize)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapPlaneSize));

    uint x = index % myHeightMapSideLength;
    uint y = index / myHeightMapSideLength;
    
    uint indexLeft = GetIndex(x - 1, y);
    uint indexRight = GetIndex(x + 1, y);
    uint indexDown = GetIndex(x, y - 1);
    uint indexUp = GetIndex(x, y + 1);

    GridHydraulicErosionCell gridHydraulicErosionCell  = gridHydraulicErosionCells[index];
    
    float totalHeightMapHeight = TotalHeightMapHeight(index);
    if(totalHeightMapHeight < mapGenerationConfiguration.SeaLevel)
    {
        gridHydraulicErosionCell.WaterHeight = mapGenerationConfiguration.SeaLevel - totalHeightMapHeight;
    }
    float totalHeight = totalHeightMapHeight + gridHydraulicErosionCell.WaterHeight;
    float outOfBoundsHeight = totalHeight - 0.2;
    if(x > 0)
    {
        float totalHeightLeft = TotalHeightMapHeight(indexLeft) + gridHydraulicErosionCells[indexLeft].WaterHeight;
        gridHydraulicErosionCell.WaterFlowLeft = max(gridHydraulicErosionCell.WaterFlowLeft + (totalHeight - totalHeightLeft) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        gridHydraulicErosionCell.SedimentFlowLeft = max(gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightLeft) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
    }
    else
    {
        if(erosionConfiguration.IsWaterKeptInBoundaries)
        {
            gridHydraulicErosionCell.WaterFlowLeft = 0.0;
            gridHydraulicErosionCell.SedimentFlowLeft = 0.0;
        }
        else
        {
            gridHydraulicErosionCell.WaterFlowLeft = max(gridHydraulicErosionCell.WaterFlowLeft + (totalHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            gridHydraulicErosionCell.SedimentFlowLeft = max(gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        }
    }

    if(x < myHeightMapSideLength - 1)
    {
        float totalHeightRight = TotalHeightMapHeight(indexRight) + gridHydraulicErosionCells[indexRight].WaterHeight;
        gridHydraulicErosionCell.WaterFlowRight = max(gridHydraulicErosionCell.WaterFlowRight + (totalHeight - totalHeightRight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        gridHydraulicErosionCell.SedimentFlowRight = max(gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightRight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
    }
    else
    {
        if(erosionConfiguration.IsWaterKeptInBoundaries)
        {
            gridHydraulicErosionCell.WaterFlowRight = 0.0;
            gridHydraulicErosionCell.SedimentFlowRight = 0.0;
        }
        else
        {
            gridHydraulicErosionCell.WaterFlowRight = max(gridHydraulicErosionCell.WaterFlowRight + (totalHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            gridHydraulicErosionCell.SedimentFlowRight = max(gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        }
    }

    if(y > 0)
    {
        float totalHeightDown = TotalHeightMapHeight(indexDown) + gridHydraulicErosionCells[indexDown].WaterHeight;
        gridHydraulicErosionCell.WaterFlowDown = max(gridHydraulicErosionCell.WaterFlowDown + (totalHeight - totalHeightDown) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        gridHydraulicErosionCell.SedimentFlowDown = max(gridHydraulicErosionCell.SedimentFlowDown + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightDown) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
    }
    else
    {
        if(erosionConfiguration.IsWaterKeptInBoundaries)
        {
            gridHydraulicErosionCell.WaterFlowDown = 0.0;
            gridHydraulicErosionCell.SedimentFlowDown = 0.0;
        }
        else
        {
            gridHydraulicErosionCell.WaterFlowDown = max(gridHydraulicErosionCell.WaterFlowDown + (totalHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            gridHydraulicErosionCell.SedimentFlowDown = max(gridHydraulicErosionCell.SedimentFlowDown + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        }
    }

    if(y < myHeightMapSideLength - 1)
    {
        float totalHeightUp = TotalHeightMapHeight(indexUp) + gridHydraulicErosionCells[indexUp].WaterHeight;
        gridHydraulicErosionCell.WaterFlowUp = max(gridHydraulicErosionCell.WaterFlowUp + (totalHeight - totalHeightUp) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        gridHydraulicErosionCell.SedimentFlowUp = max(gridHydraulicErosionCell.SedimentFlowUp + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightUp) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
    }
    else
    {
        if(erosionConfiguration.IsWaterKeptInBoundaries)
        {
            gridHydraulicErosionCell.WaterFlowUp = 0.0;
            gridHydraulicErosionCell.SedimentFlowUp = 0.0;
        }
        else
        {
            gridHydraulicErosionCell.WaterFlowUp = max(gridHydraulicErosionCell.WaterFlowUp + (totalHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            gridHydraulicErosionCell.SedimentFlowUp = max(gridHydraulicErosionCell.SedimentFlowUp + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        }
    }

    float totalFlow = gridHydraulicErosionCell.WaterFlowLeft + gridHydraulicErosionCell.WaterFlowRight + gridHydraulicErosionCell.WaterFlowDown + gridHydraulicErosionCell.WaterFlowUp;
    float scale = min(gridHydraulicErosionCell.WaterHeight / totalFlow * erosionConfiguration.TimeDelta * (1.0 - gridHydraulicErosionConfiguration.Dampening), 1.0);        
    gridHydraulicErosionCell.WaterFlowLeft *= scale;
    gridHydraulicErosionCell.WaterFlowRight *= scale;
    gridHydraulicErosionCell.WaterFlowDown *= scale;
    gridHydraulicErosionCell.WaterFlowUp *= scale;
    
    float totalSedimentFlow = gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SedimentFlowDown + gridHydraulicErosionCell.SedimentFlowUp;
    float sedimentScale = min(gridHydraulicErosionCell.SuspendedSediment / totalSedimentFlow * erosionConfiguration.TimeDelta * (1.0 - gridHydraulicErosionConfiguration.Dampening), 1.0);
    gridHydraulicErosionCell.SedimentFlowLeft *= sedimentScale;
    gridHydraulicErosionCell.SedimentFlowRight *= sedimentScale;
    gridHydraulicErosionCell.SedimentFlowDown *= sedimentScale;
    gridHydraulicErosionCell.SedimentFlowUp *= sedimentScale;

    gridHydraulicErosionCells[index] = gridHydraulicErosionCell;
    
    memoryBarrier();
}