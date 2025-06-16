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

float TotalLayerWaterHeight(uint index, uint layer)
{
    uint layerIndex = index + LayerHydraulicErosionCellsOffset(layer);
    return gridHydraulicErosionCells[layerIndex].WaterHeight;
}

float TotalLayerHeightMapAndWaterHeight(uint index, uint layer)
{
    return TotalHeightMapLayerHeight(index, layer) + TotalLayerWaterHeight(index, layer);
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

        float totalLayerHeightMapAndWaterHeight = TotalLayerHeightMapAndWaterHeight(index, layer);
        float aboveHeightMapLayerFloorHeight = HeightMapLayerFloorHeight(index, layer + 1);
        float waterFlowRight = 0.0;
        float waterFlowLeft = 0.0;
        float waterFlowUp = 0.0;
        float waterFlowDown = 0.0;
        float sedimentFlowRight = 0.0;
        float sedimentFlowLeft = 0.0;
        float sedimentFlowUp = 0.0;
        float sedimentFlowDown = 0.0;
        for(int layer2 = int(mapGenerationConfiguration.LayerCount) - 1; layer2 >= 0; layer2--)
        {
            uint layerHydraulicErosionCellsOffset2 = layer2 * myHeightMapPlaneSize;
            if(TotalLayerWaterHeight(indexLeft, layer2) > 0
                && TotalLayerHeightMapAndWaterHeight(indexLeft, layer2) > totalLayerHeightMapAndWaterHeight
                && (aboveHeightMapLayerFloorHeight == 0
                    || TotalHeightMapLayerHeight(indexLeft, layer2) < aboveHeightMapLayerFloorHeight))
            {
                if(x > 0)
                {
                    waterFlowRight += gridHydraulicErosionCells[indexLeft + layerHydraulicErosionCellsOffset2].WaterFlowRight;
                    sedimentFlowRight += gridHydraulicErosionCells[indexLeft + layerHydraulicErosionCellsOffset2].SedimentFlowRight;
                }
            }

            if(TotalLayerWaterHeight(indexRight, layer2) > 0
                && TotalLayerHeightMapAndWaterHeight(indexRight, layer2) > totalLayerHeightMapAndWaterHeight
                && (aboveHeightMapLayerFloorHeight == 0
                    || TotalHeightMapLayerHeight(indexRight, layer2) < aboveHeightMapLayerFloorHeight))
            {
		        if(x < myHeightMapSideLength - 1)
                {
                    waterFlowLeft += gridHydraulicErosionCells[indexRight + layerHydraulicErosionCellsOffset2].WaterFlowLeft;
                    sedimentFlowLeft += gridHydraulicErosionCells[indexRight + layerHydraulicErosionCellsOffset2].SedimentFlowLeft;
                }
            }

            if(TotalLayerWaterHeight(indexDown, layer2) > 0
                && TotalLayerHeightMapAndWaterHeight(indexDown, layer2) > totalLayerHeightMapAndWaterHeight
                && (aboveHeightMapLayerFloorHeight == 0
                    || TotalHeightMapLayerHeight(indexDown, layer2) < aboveHeightMapLayerFloorHeight))
            {
                if(y > 0)
                {
                    waterFlowUp += gridHydraulicErosionCells[indexDown + layerHydraulicErosionCellsOffset2].WaterFlowUp;
                    sedimentFlowUp += gridHydraulicErosionCells[indexDown + layerHydraulicErosionCellsOffset2].SedimentFlowUp;
                }
            }

            if(TotalLayerWaterHeight(indexUp, layer2) > 0
                && TotalLayerHeightMapAndWaterHeight(indexUp, layer2) > totalLayerHeightMapAndWaterHeight
                && (aboveHeightMapLayerFloorHeight == 0
                    || TotalHeightMapLayerHeight(indexUp, layer2) < aboveHeightMapLayerFloorHeight))
            {
		        if(y < myHeightMapSideLength - 1)
                {
                    waterFlowDown += gridHydraulicErosionCells[indexUp + layerHydraulicErosionCellsOffset2].WaterFlowDown;
                    sedimentFlowDown += gridHydraulicErosionCells[indexUp + layerHydraulicErosionCellsOffset2].SedimentFlowDown;
                }
            }
        }
        
        uint layerHydraulicErosionCellsOffset = layer * myHeightMapPlaneSize;
        GridHydraulicErosionCell gridHydraulicErosionCell  = gridHydraulicErosionCells[index + layerHydraulicErosionCellsOffset];

        float waterInflow = waterFlowRight + waterFlowLeft + waterFlowUp + waterFlowDown;
        float waterOutflow = gridHydraulicErosionCell.WaterFlowRight + gridHydraulicErosionCell.WaterFlowLeft + gridHydraulicErosionCell.WaterFlowUp + gridHydraulicErosionCell.WaterFlowDown;
	    float waterVolumeDelta = (waterInflow - waterOutflow) * erosionConfiguration.DeltaTime;
	    gridHydraulicErosionCell.WaterHeight = max(gridHydraulicErosionCell.WaterHeight + waterVolumeDelta, 0.0);
    
        float sedimentInflow = sedimentFlowRight + sedimentFlowLeft + sedimentFlowUp + sedimentFlowDown;
        float sedimentOutflow = gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SedimentFlowUp + gridHydraulicErosionCell.SedimentFlowDown;
	    float sedimentVolumeDelta = (sedimentInflow - sedimentOutflow) * erosionConfiguration.DeltaTime;
	    gridHydraulicErosionCell.SuspendedSediment = max(gridHydraulicErosionCell.SuspendedSediment + sedimentVolumeDelta, 0.0);
        
        gridHydraulicErosionCell.WaterHeight = max(gridHydraulicErosionCell.WaterHeight * (1.0 - gridHydraulicErosionConfiguration.EvaporationRate * erosionConfiguration.DeltaTime), 0.0);

        if(gridHydraulicErosionCell.WaterHeight > 0.0)
        {
            gridHydraulicErosionCell.WaterVelocity = 0.5 * vec2(((gridHydraulicErosionCell.WaterFlowRight - waterFlowLeft) - (gridHydraulicErosionCell.WaterFlowLeft - waterFlowRight)) * mapGenerationConfiguration.HeightMultiplier,
                                                            ((gridHydraulicErosionCell.WaterFlowUp - waterFlowDown) - (gridHydraulicErosionCell.WaterFlowDown - waterFlowUp)) * mapGenerationConfiguration.HeightMultiplier);
        }
        else
        {
            gridHydraulicErosionCell.WaterVelocity = vec2(0.0);
        }
    
        gridHydraulicErosionCells[index + layerHydraulicErosionCellsOffset] = gridHydraulicErosionCell;
    }

    memoryBarrier();
}