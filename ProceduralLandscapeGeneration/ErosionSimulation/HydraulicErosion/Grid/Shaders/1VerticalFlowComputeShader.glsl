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

float HeightMapLayerFloorHeight(uint index, uint layer)
{
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

float TotalHeightMapLayerHeight(uint index, uint layer)
{
    if(layer > 0
        && HeightMapLayerFloorHeight(index, layer) == 0)
    {
        return 0;
    }
    float height = 0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapPlaneSize + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize];
    }
    return height;
}

float TotalHeightMapHeight(uint index, uint layer)
{
    float height = 0;
    if(layer > 0)
    {
        height = heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
        if(height == 0)
        {
            return -1;
        }
    }
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapPlaneSize + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize];
    }
    return height;
}

uint GetIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

float RemainingLayerHeight(uint index, uint layer)
{
    if(layer >= mapGenerationConfiguration.LayerCount - 1)
    {
        return 1.0;
    }
    float heightMapLayerCeilingHeight = HeightMapLayerFloorHeight(index, layer + 1);
    float totalHeightMapLayerHeight = TotalHeightMapLayerHeight(index, layer);
    return heightMapLayerCeilingHeight - totalHeightMapLayerHeight;
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
    
    uint leftIndex = GetIndex(x - 1, y);
    uint rightIndex = GetIndex(x + 1, y);
    uint downIndex = GetIndex(x, y - 1);
    uint upIndex = GetIndex(x, y + 1);
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        uint gridHydraulicErosionCellIndexOffset = layer * myHeightMapPlaneSize;
        GridHydraulicErosionCell gridHydraulicErosionCell  = gridHydraulicErosionCells[index + gridHydraulicErosionCellIndexOffset];
    
        float totalHeightMapHeight = TotalHeightMapHeight(index, layer);
        if(totalHeightMapHeight > -1
            && totalHeightMapHeight < mapGenerationConfiguration.SeaLevel)
        {
            //gridHydraulicErosionCell.WaterHeight = mapGenerationConfiguration.SeaLevel - totalHeightMapHeight;
        }
        float heightMapLayerHeight = TotalHeightMapLayerHeight(index, layer);
        float totalHeight = heightMapLayerHeight + gridHydraulicErosionCell.WaterHeight;
        float outOfBoundsHeight = totalHeight - 0.2;
        if(x > 0)
        {
            float totalHeightLeft = TotalHeightMapLayerHeight(leftIndex, layer) + gridHydraulicErosionCells[leftIndex + gridHydraulicErosionCellIndexOffset].WaterHeight;
            float remainingLayerHeightLeft = RemainingLayerHeight(leftIndex, layer);
            gridHydraulicErosionCell.WaterFlowLeft = clamp(gridHydraulicErosionCell.WaterFlowLeft + (totalHeight - totalHeightLeft) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0, remainingLayerHeightLeft);
            gridHydraulicErosionCell.SedimentFlowLeft = clamp(gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightLeft) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0, remainingLayerHeightLeft);
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
            float totalHeightRight = TotalHeightMapLayerHeight(rightIndex, layer) + gridHydraulicErosionCells[rightIndex + gridHydraulicErosionCellIndexOffset].WaterHeight;
            float remainingLayerHeightRight = RemainingLayerHeight(leftIndex, layer);
            gridHydraulicErosionCell.WaterFlowRight = clamp(gridHydraulicErosionCell.WaterFlowRight + (totalHeight - totalHeightRight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0, remainingLayerHeightRight);
            gridHydraulicErosionCell.SedimentFlowRight = clamp(gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightRight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0, remainingLayerHeightRight);
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
            float totalHeightDown = TotalHeightMapLayerHeight(downIndex, layer) + gridHydraulicErosionCells[downIndex + gridHydraulicErosionCellIndexOffset].WaterHeight;
            float remainingLayerHeightDown = RemainingLayerHeight(leftIndex, layer);
            gridHydraulicErosionCell.WaterFlowDown = clamp(gridHydraulicErosionCell.WaterFlowDown + (totalHeight - totalHeightDown) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0, remainingLayerHeightDown);
            gridHydraulicErosionCell.SedimentFlowDown = clamp(gridHydraulicErosionCell.SedimentFlowDown + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightDown) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0, remainingLayerHeightDown);
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
            float totalHeightUp = TotalHeightMapLayerHeight(upIndex, layer) + gridHydraulicErosionCells[upIndex + gridHydraulicErosionCellIndexOffset].WaterHeight;
            float remainingLayerHeightUp = RemainingLayerHeight(leftIndex, layer);
            gridHydraulicErosionCell.WaterFlowUp = clamp(gridHydraulicErosionCell.WaterFlowUp + (totalHeight - totalHeightUp) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0, remainingLayerHeightUp);
            gridHydraulicErosionCell.SedimentFlowUp = clamp(gridHydraulicErosionCell.SedimentFlowUp + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightUp) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0, remainingLayerHeightUp);
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

        gridHydraulicErosionCells[index + gridHydraulicErosionCellIndexOffset] = gridHydraulicErosionCell;
    }
    
    memoryBarrier();
}