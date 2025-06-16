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
    bool AreLayerColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

struct ErosionConfiguration
{
    float DeltaTime;
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

uint myHeightMapSideLength;
uint myHeightMapPlaneSize;

uint GetIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

uint LayerHydraulicErosionCellsOffset(uint layer)
{
    return layer * myHeightMapPlaneSize;
}

float HeightMapLayerFloorHeight(uint index, uint layer)
{
    if(layer < 1
        || layer >= mapGenerationConfiguration.LayerCount)
    {
        return 0.0;
    }
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

uint HeightMapLayerOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

uint HeightMapRockTypeOffset(uint rockType)
{
    return rockType * myHeightMapPlaneSize;
}

float HeightMapLayerHeight(uint index, uint layer)
{
    float heightMapLayerHeight = 0.0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        heightMapLayerHeight += heightMap[index + HeightMapRockTypeOffset(rockType) + HeightMapLayerOffset(layer)];
    }
    return heightMapLayerHeight;
}

float TotalHeightMapLayerHeight(uint index, uint layer)
{
    float heightMapLayerFloorHeight = 0.0;
    if(layer > 0)
    {
        heightMapLayerFloorHeight = HeightMapLayerFloorHeight(index, layer);
        if(heightMapLayerFloorHeight == 0)
        {
            return 0.0;
        }
    }
    return heightMapLayerFloorHeight + HeightMapLayerHeight(index, layer);
}

float ReachableNeighborHeightMapHeight(uint neighborIndex, float heightMapHeight, float heightMapAndWaterHeight)
{
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        if(layer > 0
            && HeightMapLayerFloorHeight(neighborIndex, layer) == 0)
        {
            continue;
        }
        float neighborTotalHeightMapLayerHeight = TotalHeightMapLayerHeight(neighborIndex, layer);
        float neighborAboveHeightMapLayerFloorHeight = HeightMapLayerFloorHeight(neighborIndex, layer + 1);
        if(layer > 0 && neighborTotalHeightMapLayerHeight > 0 && neighborTotalHeightMapLayerHeight < heightMapAndWaterHeight && (neighborAboveHeightMapLayerFloorHeight == 0 || neighborAboveHeightMapLayerFloorHeight > heightMapHeight))
        {
            return neighborTotalHeightMapLayerHeight;
        }
        else if(layer == 0 && neighborTotalHeightMapLayerHeight >= 0 && neighborTotalHeightMapLayerHeight < heightMapAndWaterHeight && (neighborAboveHeightMapLayerFloorHeight == 0 || neighborAboveHeightMapLayerFloorHeight > heightMapHeight))
        {
            return neighborTotalHeightMapLayerHeight;
        }
    }
    return 100.0;
}

float LayerSplitSize(uint index, uint layer)
{
    float aboveHeightMapLayerFloorHeight = HeightMapLayerFloorHeight(index, layer + 1);
    if(layer == 0
        && aboveHeightMapLayerFloorHeight > 0.0)
    {
        float totalHeightMapLayerHeight = TotalHeightMapLayerHeight(index, layer);
        return aboveHeightMapLayerFloorHeight - totalHeightMapLayerHeight;
    }
    return 100.0;
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
    
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        if(layer > 0
            && HeightMapLayerFloorHeight(index, layer) == 0)
        {
            continue;
        }

        uint layerIndex = index + LayerHydraulicErosionCellsOffset(layer);
        GridHydraulicErosionCell gridHydraulicErosionCell  = gridHydraulicErosionCells[layerIndex];
    
        float totalHeightMapLayerHeight = TotalHeightMapLayerHeight(index, layer);
        if(totalHeightMapLayerHeight < mapGenerationConfiguration.SeaLevel)
        {
            float layerSplitSize = LayerSplitSize(index, layer);
            gridHydraulicErosionCell.WaterHeight = min(mapGenerationConfiguration.SeaLevel - totalHeightMapLayerHeight, layerSplitSize);
        }

        if(gridHydraulicErosionCell.WaterHeight == 0)
        {
            continue;
        }

        float totalLayerHeightMapAndWaterHeight = totalHeightMapLayerHeight + gridHydraulicErosionCell.WaterHeight;
        float outOfBoundsHeight = 0.0;
        if(x > 0)
        {
            float totalHeightMapLayerHeightLeft = ReachableNeighborHeightMapHeight(indexLeft, totalHeightMapLayerHeight, totalLayerHeightMapAndWaterHeight);
            float totalLayerHeightMapAndWaterHeightLeft = totalHeightMapLayerHeightLeft + gridHydraulicErosionCells[indexLeft].WaterHeight;
            gridHydraulicErosionCell.WaterFlowLeft = max((totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightLeft) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
            gridHydraulicErosionCell.SedimentFlowLeft = max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightLeft) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
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
                gridHydraulicErosionCell.WaterFlowLeft = max((totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
                gridHydraulicErosionCell.SedimentFlowLeft = max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
            }
        }

        if(x < myHeightMapSideLength - 1)
        {
            float totalHeightMapLayerHeightRight = ReachableNeighborHeightMapHeight(indexRight, totalHeightMapLayerHeight, totalLayerHeightMapAndWaterHeight);
            float totalLayerHeightMapAndWaterHeightRight = totalHeightMapLayerHeightRight + gridHydraulicErosionCells[indexRight].WaterHeight;
            gridHydraulicErosionCell.WaterFlowRight = max((totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightRight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
            gridHydraulicErosionCell.SedimentFlowRight = max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightRight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
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
                gridHydraulicErosionCell.WaterFlowRight = max((totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
                gridHydraulicErosionCell.SedimentFlowRight = max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
            }
        }

        if(y > 0)
        {
            float totalHeightMapLayerHeightDown = ReachableNeighborHeightMapHeight(indexDown, totalHeightMapLayerHeight, totalLayerHeightMapAndWaterHeight);
            float totalLayerHeightMapAndWaterHeightDown = totalHeightMapLayerHeightDown + gridHydraulicErosionCells[indexDown].WaterHeight;
            gridHydraulicErosionCell.WaterFlowDown = max((totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightDown) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
            gridHydraulicErosionCell.SedimentFlowDown = max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightDown) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
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
                gridHydraulicErosionCell.WaterFlowDown = max((totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
                gridHydraulicErosionCell.SedimentFlowDown = max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
            }
        }

        if(y < myHeightMapSideLength - 1)
        {
            float totalHeightMapLayerHeightUp = ReachableNeighborHeightMapHeight(indexUp, totalHeightMapLayerHeight, totalLayerHeightMapAndWaterHeight);
            float totalLayerHeightMapAndWaterHeightUp = totalHeightMapLayerHeightUp + gridHydraulicErosionCells[indexUp].WaterHeight;
            gridHydraulicErosionCell.WaterFlowUp = max((totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightUp) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
            gridHydraulicErosionCell.SedimentFlowUp = max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightUp) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
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
                gridHydraulicErosionCell.WaterFlowUp = max((totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
                gridHydraulicErosionCell.SedimentFlowUp = max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.DeltaTime, 0.0);
            }
        }

        float totalFlow = gridHydraulicErosionCell.WaterFlowLeft + gridHydraulicErosionCell.WaterFlowRight + gridHydraulicErosionCell.WaterFlowDown + gridHydraulicErosionCell.WaterFlowUp;
        float scale = min(gridHydraulicErosionCell.WaterHeight / totalFlow * erosionConfiguration.DeltaTime * (1.0 - gridHydraulicErosionConfiguration.Dampening), 1.0);
        gridHydraulicErosionCell.WaterFlowLeft *= scale;
        gridHydraulicErosionCell.WaterFlowRight *= scale;
        gridHydraulicErosionCell.WaterFlowDown *= scale;
        gridHydraulicErosionCell.WaterFlowUp *= scale;
    
        float totalSedimentFlow = gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SedimentFlowDown + gridHydraulicErosionCell.SedimentFlowUp;
        float sedimentScale = min(gridHydraulicErosionCell.SuspendedSediment / totalSedimentFlow * erosionConfiguration.DeltaTime * (1.0 - gridHydraulicErosionConfiguration.Dampening), 1.0);
        gridHydraulicErosionCell.SedimentFlowLeft *= sedimentScale;
        gridHydraulicErosionCell.SedimentFlowRight *= sedimentScale;
        gridHydraulicErosionCell.SedimentFlowDown *= sedimentScale;
        gridHydraulicErosionCell.SedimentFlowUp *= sedimentScale;

        gridHydraulicErosionCells[layerIndex] = gridHydraulicErosionCell;
    }
    
    memoryBarrier();
}