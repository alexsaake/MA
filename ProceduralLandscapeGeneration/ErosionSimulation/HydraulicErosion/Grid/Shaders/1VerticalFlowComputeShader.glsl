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
    bool AreLayerColorsEnabled;
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

float LayerHeightMapFloorHeight(uint index, uint layer)
{
    if(layer < 1
        || layer >= mapGenerationConfiguration.LayerCount)
    {
        return 0.0;
    }
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

uint LayerHeightMapOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

float LayerHeightMapRockTypeHeight(uint index, uint layer)
{
    float layerHeightMapRockTypeHeight = 0.0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        layerHeightMapRockTypeHeight += heightMap[index + rockType * myHeightMapPlaneSize + LayerHeightMapOffset(layer)];
    }
    return layerHeightMapRockTypeHeight;
}

float TotalLayerHeightMapHeight(uint index, uint layer)
{
    float layerHeightMapFloorHeight = 0.0;
    if(layer > 0)
    {
        layerHeightMapFloorHeight = LayerHeightMapFloorHeight(index, layer);
        if(layerHeightMapFloorHeight == 0)
        {
            return 0.0;
        }
    }
    return layerHeightMapFloorHeight + LayerHeightMapRockTypeHeight(index, layer);
}

float ReachableNeighborHeightMapHeight(uint neighborIndex, float heightMapHeight, float heightMapAndWaterHeight)
{
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        float neighborTotalLayerHeightMapHeight = TotalLayerHeightMapHeight(neighborIndex, layer);
        float neighborAboveLayerHeightMapFloorHeight = LayerHeightMapFloorHeight(neighborIndex, layer + 1);
        if(layer > 0 && neighborTotalLayerHeightMapHeight > 0 && neighborTotalLayerHeightMapHeight < heightMapAndWaterHeight && (neighborAboveLayerHeightMapFloorHeight == 0 || neighborAboveLayerHeightMapFloorHeight > heightMapHeight))
        {
            return neighborTotalLayerHeightMapHeight;
        }
        else if(layer == 0 && neighborTotalLayerHeightMapHeight >= 0 && neighborTotalLayerHeightMapHeight < heightMapAndWaterHeight && (neighborAboveLayerHeightMapFloorHeight == 0 || neighborAboveLayerHeightMapFloorHeight > heightMapHeight))
        {
            return neighborTotalLayerHeightMapHeight;
        }
    }
    return 100;
}

float LayerSplitSize(uint index, uint layer)
{
    float aboveLayerHeightMapFloorHeight = LayerHeightMapFloorHeight(index, layer + 1);
    if(layer == 0
        && aboveLayerHeightMapFloorHeight > 0.0)
    {
        float totalLayerHeightMapHeight = TotalLayerHeightMapHeight(index, layer);
        return aboveLayerHeightMapFloorHeight - totalLayerHeightMapHeight;
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
            && LayerHeightMapFloorHeight(index, layer) == 0)
        {
            continue;
        }

        uint layerIndex = index + LayerHydraulicErosionCellsOffset(layer);
        GridHydraulicErosionCell gridHydraulicErosionCell  = gridHydraulicErosionCells[layerIndex];
    
        float totalLayerHeightMapHeight = TotalLayerHeightMapHeight(index, layer);
        if(totalLayerHeightMapHeight < mapGenerationConfiguration.SeaLevel)
        {
            float layerSplitSize = LayerSplitSize(index, layer);
            gridHydraulicErosionCell.WaterHeight = min(mapGenerationConfiguration.SeaLevel - totalLayerHeightMapHeight, layerSplitSize);
        }

        if(gridHydraulicErosionCell.WaterHeight == 0)
        {
            continue;
        }

        float totalLayerHeightMapAndWaterHeight = totalLayerHeightMapHeight + gridHydraulicErosionCell.WaterHeight;
        float outOfBoundsHeight = 0.0;
        if(x > 0)
        {
            float totalLayerHeightMapHeightLeft = ReachableNeighborHeightMapHeight(indexLeft, totalLayerHeightMapHeight, totalLayerHeightMapAndWaterHeight);
            float totalLayerHeightMapAndWaterHeightLeft = totalLayerHeightMapHeightLeft + gridHydraulicErosionCells[indexLeft].WaterHeight;
            float layerSplitSizeLeft = LayerSplitSize(indexLeft, layer);
            gridHydraulicErosionCell.WaterFlowLeft = min(max((totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightLeft) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeLeft);
            gridHydraulicErosionCell.SedimentFlowLeft = min(max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightLeft) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeLeft);
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
                float layerSplitSizeLeft = LayerSplitSize(indexLeft, layer);
                gridHydraulicErosionCell.WaterFlowLeft = min(max((totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeLeft);
                gridHydraulicErosionCell.SedimentFlowLeft = min(max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeLeft);
            }
        }

        if(x < myHeightMapSideLength - 1)
        {
            float totalLayerHeightMapHeightRight = ReachableNeighborHeightMapHeight(indexRight, totalLayerHeightMapHeight, totalLayerHeightMapAndWaterHeight);
            float totalLayerHeightMapAndWaterHeightRight = totalLayerHeightMapHeightRight + gridHydraulicErosionCells[indexRight].WaterHeight;
            float layerSplitSizeRight = LayerSplitSize(indexRight, layer);
            gridHydraulicErosionCell.WaterFlowRight = min(max((totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightRight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeRight);
            gridHydraulicErosionCell.SedimentFlowRight = min(max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightRight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeRight);
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
                float layerSplitSizeRight = LayerSplitSize(indexRight, layer);
                gridHydraulicErosionCell.WaterFlowRight = min(max((totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeRight);
                gridHydraulicErosionCell.SedimentFlowRight = min(max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeRight);
            }
        }

        if(y > 0)
        {
            float totalLayerHeightMapHeightDown = ReachableNeighborHeightMapHeight(indexDown, totalLayerHeightMapHeight, totalLayerHeightMapAndWaterHeight);
            float totalLayerHeightMapAndWaterHeightDown = totalLayerHeightMapHeightDown + gridHydraulicErosionCells[indexDown].WaterHeight;
            float layerSplitSizeDown = LayerSplitSize(indexDown, layer);
            gridHydraulicErosionCell.WaterFlowDown = min(max((totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightDown) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeDown);
            gridHydraulicErosionCell.SedimentFlowDown = min(max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightDown) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeDown);
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
                float layerSplitSizeDown = LayerSplitSize(indexDown, layer);
                gridHydraulicErosionCell.WaterFlowDown = min(max((totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeDown);
                gridHydraulicErosionCell.SedimentFlowDown = min(max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeDown);
            }
        }

        if(y < myHeightMapSideLength - 1)
        {
            float totalLayerHeightMapHeightUp = ReachableNeighborHeightMapHeight(indexUp, totalLayerHeightMapHeight, totalLayerHeightMapAndWaterHeight);
            float totalLayerHeightMapAndWaterHeightUp = totalLayerHeightMapHeightUp + gridHydraulicErosionCells[indexUp].WaterHeight;
            float layerSplitSizeUp = LayerSplitSize(indexUp, layer);
            gridHydraulicErosionCell.WaterFlowUp = min(max((totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightUp) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeUp);
            gridHydraulicErosionCell.SedimentFlowUp = min(max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - totalLayerHeightMapAndWaterHeightUp) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeUp);
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
                float layerSplitSizeUp = LayerSplitSize(indexUp, layer);
                gridHydraulicErosionCell.WaterFlowUp = min(max((totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeUp);
                gridHydraulicErosionCell.SedimentFlowUp = min(max(gridHydraulicErosionCell.SuspendedSediment * (totalLayerHeightMapAndWaterHeight - outOfBoundsHeight) * gridHydraulicErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0), layerSplitSizeUp);
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

        gridHydraulicErosionCells[layerIndex] = gridHydraulicErosionCell;
    }
    
    memoryBarrier();
}