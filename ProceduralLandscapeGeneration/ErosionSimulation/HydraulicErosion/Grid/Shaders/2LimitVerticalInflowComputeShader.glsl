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

void main()
{
    uint index = gl_GlobalInvocationID.x;
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= myHeightMapPlaneSize
        || mapGenerationConfiguration.LayerCount < 2)
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
    
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        float layerSplitSize = LayerSplitSize(index, layer);
        if(layerSplitSize == 100.0
            || (layer > 0
                && HeightMapLayerFloorHeight(index, layer) == 0))
        {
            continue;
        }

        float totalLayerHeightMapAndWaterHeight = TotalLayerHeightMapAndWaterHeight(index, layer);
        float aboveHeightMapLayerFloorHeight = HeightMapLayerFloorHeight(index, layer + 1);
        float layerWaterInflow = 0.0;

        for(uint layer2 = 0; layer2 < mapGenerationConfiguration.LayerCount; layer2++)
        {
            if(TotalLayerWaterHeight(indexLeft, layer2) > 0
                && TotalLayerHeightMapAndWaterHeight(indexLeft, layer2) > totalLayerHeightMapAndWaterHeight
                && (aboveHeightMapLayerFloorHeight == 0
                    || TotalHeightMapLayerHeight(indexLeft, layer2) < aboveHeightMapLayerFloorHeight))
            {
                if(x > 0)
                {
                    layerWaterInflow += gridHydraulicErosionCells[indexLeft + LayerHydraulicErosionCellsOffset(layer2)].WaterFlowRight;
                }
            }

            if(TotalLayerWaterHeight(indexRight, layer2) > 0
                && TotalLayerHeightMapAndWaterHeight(indexRight, layer2) > totalLayerHeightMapAndWaterHeight
                && (aboveHeightMapLayerFloorHeight == 0
                    || TotalHeightMapLayerHeight(indexRight, layer2) < aboveHeightMapLayerFloorHeight))
            {
		        if(x < myHeightMapSideLength - 1)
                {
                    layerWaterInflow += gridHydraulicErosionCells[indexRight + LayerHydraulicErosionCellsOffset(layer2)].WaterFlowLeft;
                }
            }

            if(TotalLayerWaterHeight(indexDown, layer2) > 0
                && TotalLayerHeightMapAndWaterHeight(indexDown, layer2) > totalLayerHeightMapAndWaterHeight
                && (aboveHeightMapLayerFloorHeight == 0
                    || TotalHeightMapLayerHeight(indexDown, layer2) < aboveHeightMapLayerFloorHeight))
            {
                if(y > 0)
                {
                    layerWaterInflow += gridHydraulicErosionCells[indexDown + LayerHydraulicErosionCellsOffset(layer2)].WaterFlowUp;
                }
            }

            if(TotalLayerWaterHeight(indexUp, layer2) > 0
                && TotalLayerHeightMapAndWaterHeight(indexUp, layer2) > totalLayerHeightMapAndWaterHeight
                && (aboveHeightMapLayerFloorHeight == 0
                    || TotalHeightMapLayerHeight(indexUp, layer2) < aboveHeightMapLayerFloorHeight))
            {
		        if(y < myHeightMapSideLength - 1)
                {
                    layerWaterInflow += gridHydraulicErosionCells[indexUp + LayerHydraulicErosionCellsOffset(layer2)].WaterFlowDown;
                }
            }
        }

        if(layerWaterInflow > layerSplitSize)
        {
            float scale = min(layerSplitSize / layerWaterInflow, 1.0);
            for(uint layer2 = 0; layer2 < mapGenerationConfiguration.LayerCount; layer2++)
            {
                if(TotalLayerWaterHeight(indexLeft, layer2) > 0
                    && TotalLayerHeightMapAndWaterHeight(indexLeft, layer2) > totalLayerHeightMapAndWaterHeight
                    && (aboveHeightMapLayerFloorHeight == 0
                        || TotalHeightMapLayerHeight(indexLeft, layer2) < aboveHeightMapLayerFloorHeight))
                {
                    if(x > 0)
                    {
                        gridHydraulicErosionCells[indexLeft + LayerHydraulicErosionCellsOffset(layer2)].WaterFlowRight *= scale;
                        gridHydraulicErosionCells[indexLeft + LayerHydraulicErosionCellsOffset(layer2)].SedimentFlowRight *= scale;
                    }
                }

                if(TotalLayerWaterHeight(indexRight, layer2) > 0
                    && TotalLayerHeightMapAndWaterHeight(indexRight, layer2) > totalLayerHeightMapAndWaterHeight
                    && (aboveHeightMapLayerFloorHeight == 0
                        || TotalHeightMapLayerHeight(indexRight, layer2) < aboveHeightMapLayerFloorHeight))
                {
		            if(x < myHeightMapSideLength - 1)
                    {
                        gridHydraulicErosionCells[indexRight + LayerHydraulicErosionCellsOffset(layer2)].WaterFlowLeft *= scale;
                        gridHydraulicErosionCells[indexRight + LayerHydraulicErosionCellsOffset(layer2)].SedimentFlowLeft *= scale;
                    }
                }

                if(TotalLayerWaterHeight(indexDown, layer2) > 0
                    && TotalLayerHeightMapAndWaterHeight(indexDown, layer2) > totalLayerHeightMapAndWaterHeight
                    && (aboveHeightMapLayerFloorHeight == 0
                        || TotalHeightMapLayerHeight(indexDown, layer2) < aboveHeightMapLayerFloorHeight))
                {
                    if(y > 0)
                    {
                        gridHydraulicErosionCells[indexDown + LayerHydraulicErosionCellsOffset(layer2)].WaterFlowUp *= scale;
                        gridHydraulicErosionCells[indexDown + LayerHydraulicErosionCellsOffset(layer2)].SedimentFlowUp *= scale;
                    }
                }

                if(TotalLayerWaterHeight(indexUp, layer2) > 0
                    && TotalLayerHeightMapAndWaterHeight(indexUp, layer2) > totalLayerHeightMapAndWaterHeight
                    && (aboveHeightMapLayerFloorHeight == 0
                        || TotalHeightMapLayerHeight(indexUp, layer2) < aboveHeightMapLayerFloorHeight))
                {
		            if(y < myHeightMapSideLength - 1)
                    {
                        gridHydraulicErosionCells[indexUp + LayerHydraulicErosionCellsOffset(layer2)].WaterFlowDown *= scale;
                        gridHydraulicErosionCells[indexUp + LayerHydraulicErosionCellsOffset(layer2)].SedimentFlowDown *= scale;
                    }
                }
            }
        }
    }

    memoryBarrier();
}