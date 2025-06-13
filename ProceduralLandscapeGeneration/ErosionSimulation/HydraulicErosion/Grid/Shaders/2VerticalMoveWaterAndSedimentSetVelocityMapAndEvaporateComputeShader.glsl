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

uint LayerHydraulicErosionCellsOffset(uint layer)
{
    return layer * myHeightMapPlaneSize;
}

float LayerHeightMapFloorHeight(uint index, uint layer)
{
    if(layer < 1
        || layer >= mapGenerationConfiguration.LayerCount - 1)
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

float TotalLayerWaterHeight(uint index, uint layer)
{
    uint layerIndex = index + LayerHydraulicErosionCellsOffset(layer);
    return gridHydraulicErosionCells[layerIndex].WaterHeight;
}

float TotalLayerHeightMapAndWaterHeight(uint index, uint layer)
{
    return TotalLayerHeightMapHeight(index, layer) + TotalLayerWaterHeight(index, layer);
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
        //TODO fix reduced flow? visualzation issue?
        if(layer > 0
            && LayerHeightMapFloorHeight(index, layer) == 0)
        {
            continue;
        }
        
        float totalLayerHeightMapAndWaterHeight = TotalLayerHeightMapAndWaterHeight(index, layer);
        float aboveLayerHeightMapFloorHeight = LayerHeightMapFloorHeight(index, layer + 1);
        float waterFlowRight = 0.0;
        float waterFlowLeft = 0.0;
        float waterFlowUp = 0.0;
        float waterFlowDown = 0.0;
        float sedimentFlowRight = 0.0;
        float sedimentFlowLeft = 0.0;
        float sedimentFlowUp = 0.0;
        float sedimentFlowDown = 0.0;
        for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
        {
            if(TotalLayerWaterHeight(indexLeft, layer) > 0
                && TotalLayerHeightMapAndWaterHeight(indexLeft, layer) > totalLayerHeightMapAndWaterHeight
                && (aboveLayerHeightMapFloorHeight == 0
                    || TotalLayerHeightMapHeight(indexLeft, layer) < aboveLayerHeightMapFloorHeight))
            {
                waterFlowRight += gridHydraulicErosionCells[indexLeft + LayerHydraulicErosionCellsOffset(layer)].WaterFlowRight;
                sedimentFlowRight += gridHydraulicErosionCells[indexLeft + LayerHydraulicErosionCellsOffset(layer)].SedimentFlowRight;
            }

            if(TotalLayerWaterHeight(indexRight, layer) > 0
                && TotalLayerHeightMapAndWaterHeight(indexRight, layer) > totalLayerHeightMapAndWaterHeight
                && (aboveLayerHeightMapFloorHeight == 0
                    || TotalLayerHeightMapHeight(indexRight, layer) < aboveLayerHeightMapFloorHeight))
            {
                waterFlowLeft += gridHydraulicErosionCells[indexRight + LayerHydraulicErosionCellsOffset(layer)].WaterFlowLeft;
                sedimentFlowLeft += gridHydraulicErosionCells[indexRight + LayerHydraulicErosionCellsOffset(layer)].SedimentFlowLeft;
            }

            if(TotalLayerWaterHeight(indexDown, layer) > 0
                && TotalLayerHeightMapAndWaterHeight(indexDown, layer) > totalLayerHeightMapAndWaterHeight
                && (aboveLayerHeightMapFloorHeight == 0
                    || TotalLayerHeightMapHeight(indexDown, layer) < aboveLayerHeightMapFloorHeight))
            {
                waterFlowUp += gridHydraulicErosionCells[indexDown + LayerHydraulicErosionCellsOffset(layer)].WaterFlowUp;
                sedimentFlowUp += gridHydraulicErosionCells[indexDown + LayerHydraulicErosionCellsOffset(layer)].SedimentFlowUp;
            }

            if(TotalLayerWaterHeight(indexUp, layer) > 0
                && TotalLayerHeightMapAndWaterHeight(indexUp, layer) > totalLayerHeightMapAndWaterHeight
                && (aboveLayerHeightMapFloorHeight == 0
                    || TotalLayerHeightMapHeight(indexUp, layer) < aboveLayerHeightMapFloorHeight))
            {
                waterFlowDown += gridHydraulicErosionCells[indexUp + LayerHydraulicErosionCellsOffset(layer)].WaterFlowDown;
                sedimentFlowDown += gridHydraulicErosionCells[indexUp + LayerHydraulicErosionCellsOffset(layer)].SedimentFlowDown;
            }
        }        

        uint layerIndex = index + LayerHydraulicErosionCellsOffset(layer);
        GridHydraulicErosionCell gridHydraulicErosionCell  = gridHydraulicErosionCells[layerIndex];

        float waterFlowIn = waterFlowRight + waterFlowLeft + waterFlowUp + waterFlowDown;
        float waterFlowOut = gridHydraulicErosionCell.WaterFlowRight + gridHydraulicErosionCell.WaterFlowLeft + gridHydraulicErosionCell.WaterFlowUp + gridHydraulicErosionCell.WaterFlowDown;
	    float waterVolumeDelta = (waterFlowIn - waterFlowOut) * erosionConfiguration.TimeDelta;
	    gridHydraulicErosionCell.WaterHeight = max(gridHydraulicErosionCell.WaterHeight + waterVolumeDelta, 0.0);
    
        float sedimentFlowIn = sedimentFlowRight + sedimentFlowLeft + sedimentFlowUp + sedimentFlowDown;
        float sedimentFlowOut = gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SedimentFlowUp + gridHydraulicErosionCell.SedimentFlowDown;
	    float sedimentVolumeDelta = (sedimentFlowIn - sedimentFlowOut) * erosionConfiguration.TimeDelta;
	    gridHydraulicErosionCell.SuspendedSediment = max(gridHydraulicErosionCell.SuspendedSediment + sedimentVolumeDelta, 0.0);

        if(gridHydraulicErosionCell.WaterHeight > 0.0
            && x > 0 && x < myHeightMapSideLength - 1
            && y > 0 && y < myHeightMapSideLength - 1)
        {
            gridHydraulicErosionCell.WaterVelocity = 0.5 * vec2(((gridHydraulicErosionCell.WaterFlowRight - waterFlowLeft) - (gridHydraulicErosionCell.WaterFlowLeft - waterFlowRight)) * mapGenerationConfiguration.HeightMultiplier,
                                                            ((gridHydraulicErosionCell.WaterFlowUp - waterFlowDown) - (gridHydraulicErosionCell.WaterFlowDown - waterFlowUp)) * mapGenerationConfiguration.HeightMultiplier);
        }
        else
        {
            gridHydraulicErosionCell.WaterVelocity = vec2(0.0);
        }
        gridHydraulicErosionCell.WaterHeight = max(gridHydraulicErosionCell.WaterHeight * (1.0 - gridHydraulicErosionConfiguration.EvaporationRate * erosionConfiguration.TimeDelta), 0.0);
    
        gridHydraulicErosionCells[layerIndex] = gridHydraulicErosionCell;
    }

    memoryBarrier();
}