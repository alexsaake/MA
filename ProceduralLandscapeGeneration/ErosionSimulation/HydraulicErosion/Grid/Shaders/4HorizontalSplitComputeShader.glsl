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

struct RockTypeConfiguration
{
    float Hardness;
    float TangensAngleOfRepose;
    float CollapseThreshold;
};

layout(std430, binding = 18) buffer rockTypesConfigurationShaderBuffer
{
    RockTypeConfiguration[] rockTypesConfiguration;
};

uint myHeightMapSideLength;
uint myHeightMapPlaneSize;

uint GetIndex(uint x, uint y)
{
    return uint((y * myHeightMapSideLength) + x);
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
    float heightMapFloorHeight = 0.0;
    float rockTypeHeight = 0.0;
    if(layer > 0)
    {
        heightMapFloorHeight = LayerHeightMapFloorHeight(index, layer);
        if(heightMapFloorHeight == 0)
        {
            return 0.0;
        }
    }
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        rockTypeHeight += heightMap[index + rockType * myHeightMapPlaneSize + LayerHeightMapOffset(layer)];
    }
    return heightMapFloorHeight + rockTypeHeight;
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
            heightMapFloorHeight = LayerHeightMapFloorHeight(index, layer);
            if(heightMapFloorHeight == 0)
            {
                continue;
            }
        }
        for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
        {
            rockTypeHeight += heightMap[index + rockType * myHeightMapPlaneSize + LayerHeightMapOffset(layer)];
        }
        if(rockTypeHeight > 0)
        {
            return heightMapFloorHeight + rockTypeHeight;
        }
    }
    return heightMapFloorHeight + rockTypeHeight;
}

void MoveRockToAboveLayer(uint index, float splitHeight)
{
    float sedimentToFill = splitHeight;
    for(int rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        uint currentLayerRockTypeHeightMapIndex = index + rockType * myHeightMapPlaneSize;
        uint aboveLayerRockTypeHeightMapIndex = index + rockType * myHeightMapPlaneSize + (mapGenerationConfiguration.RockTypeCount + 1) * myHeightMapPlaneSize;
        float currentLayerRockTypeHeight = heightMap[currentLayerRockTypeHeightMapIndex];
        sedimentToFill -= currentLayerRockTypeHeight;
        if(sedimentToFill < 0)
        {
            heightMap[currentLayerRockTypeHeightMapIndex] += sedimentToFill;
            heightMap[aboveLayerRockTypeHeightMapIndex] -= sedimentToFill;
            sedimentToFill = 0;
        }
    }
}

uint LayerHydraulicErosionCellsOffset(uint layer)
{
    return layer * myHeightMapPlaneSize;
}

void MoveWaterAndSuspendedSedimentToAboveLayer(uint index, uint layer)
{
    if(layer >= mapGenerationConfiguration.LayerCount - 1)
    {
        return;
    }
    uint currentLayerGridHydraulicErosionCellIndex = index + LayerHydraulicErosionCellsOffset(layer);
    uint aboveLayerGridHydraulicErosionCellIndex = index + LayerHydraulicErosionCellsOffset(layer + 1);
    
    gridHydraulicErosionCells[aboveLayerGridHydraulicErosionCellIndex].WaterHeight += gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].WaterHeight;
    gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].WaterHeight = 0.0;
    gridHydraulicErosionCells[aboveLayerGridHydraulicErosionCellIndex].SuspendedSediment += gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].SuspendedSediment;
    gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].SuspendedSediment = 0.0;
}

void SetLayerHeightMapFloorHeight(uint index, uint layer, float value)
{
    heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize] = value;
}

float SuspendFromLayerZeroTop(uint index, float requiredSediment)
{
    float suspendedSediment = 0;
    for(int rockType = int(mapGenerationConfiguration.RockTypeCount) - 1; rockType >= 0; rockType--)
    {
        uint rockTypeIndex = index + rockType * myHeightMapPlaneSize;
        float height = heightMap[rockTypeIndex];
        float hardness = (1.0 - rockTypesConfiguration[rockType].Hardness);
        float toBeSuspendedSediment = requiredSediment * hardness;
        if(height >= toBeSuspendedSediment)
        {
            heightMap[rockTypeIndex] -= toBeSuspendedSediment;
            suspendedSediment += toBeSuspendedSediment;
            return suspendedSediment;
        }
        else if(height > 0)
        {
            float toBeSuspendedHeight = height;
            heightMap[rockTypeIndex] = 0;
            requiredSediment -= toBeSuspendedHeight;
            suspendedSediment += toBeSuspendedHeight;
        }
    }
    return suspendedSediment;
}

float SuspendFromLayerOneBottom(uint index, float requiredSediment)
{
    if(layer < 1
        || (LayerHeightMapFloorHeight(index, layer) == 0
            && layer > 0))
    {
        return 0.0;
    }
    float suspendedSediment = 0;
    for(int rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        uint rockTypeIndex = index + rockType * myHeightMapPlaneSize + (mapGenerationConfiguration.RockTypeCount + 1) * myHeightMapPlaneSize;
        uint layerFloorIndex = index + mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize;
        float height = heightMap[rockTypeIndex];
        float hardness = (1.0 - rockTypesConfiguration[rockType].Hardness);
        float toBeSuspendedSediment = requiredSediment * hardness;
        if(height >= toBeSuspendedSediment)
        {
            heightMap[rockTypeIndex] -= toBeSuspendedSediment;
            heightMap[layerFloorIndex] += toBeSuspendedSediment;
            suspendedSediment += toBeSuspendedSediment;
            return suspendedSediment;
        }
        else if(height > 0)
        {
            float toBeSuspendedHeight = height;
            heightMap[rockTypeIndex] = 0;
            heightMap[layerFloorIndex] += toBeSuspendedHeight;
            requiredSediment -= toBeSuspendedHeight;
            suspendedSediment += toBeSuspendedHeight;
        }
    }
    return suspendedSediment;
}

void SplitAt(uint index, float splitHeight)
{
    if(LayerHeightMapFloorHeight(index, 1) > 0)
    {
        return;
    }
    SetLayerHeightMapFloorHeight(index, 1, splitHeight);
    MoveRockToAboveLayer(index, splitHeight);
    MoveWaterAndSuspendedSedimentToAboveLayer(index, 0);
}

bool TrySplit(uint index, uint neighborIndex, vec2 direction)
{
    GridHydraulicErosionCell gridHydraulicErosionCell = gridHydraulicErosionCells[neighborIndex];
    
    if(gridHydraulicErosionCell.WaterHeight > 0)
    {
	    if(ceil(gridHydraulicErosionCell.WaterVelocity) - direction != 0)
        {
            float neighborTotalHeight = TotalHeightMapHeight(neighborIndex);
            float neighborTotalTerrainAndWaterHeight = neighborTotalHeight + gridHydraulicErosionCell.WaterHeight;
            float totalheight = TotalHeightMapHeight(index);
            if(neighborTotalTerrainAndWaterHeight < totalheight)
            {
                bool isEroded = false;

                float sedimentCapacity = min(gridHydraulicErosionConfiguration.SedimentCapacity * length(gridHydraulicErosionCell.WaterVelocity), 1.0);

                float layerZeroHeight = LayerHeightMapRockTypeHeight(index, 0);
                float layerZeroHeightBelowSeaLevel = max(neighborTotalTerrainAndWaterHeight - layerZeroHeight, 0.0);
	            float erosionDepthLimit = (gridHydraulicErosionConfiguration.MaximalErosionDepth - min(gridHydraulicErosionConfiguration.MaximalErosionDepth, layerZeroHeightBelowSeaLevel)) * 1.0 / gridHydraulicErosionConfiguration.MaximalErosionDepth;
                float sedimentCapacityBottom = sedimentCapacity * erosionDepthLimit;
            
                if(sedimentCapacityBottom > gridHydraulicErosionCell.SuspendedSediment)
	            {
		            float soilSuspendedBottom = max(gridHydraulicErosionConfiguration.HorizontalSuspensionRate * (sedimentCapacityBottom - gridHydraulicErosionCell.SuspendedSediment) * erosionConfiguration.TimeDelta, 0.0);

		            SplitAt(index, neighborTotalHeight);
		            float suspendedSedimentBottom = SuspendFromLayerZeroTop(index, soilSuspendedBottom);
		            gridHydraulicErosionCell.SuspendedSediment += suspendedSedimentBottom;

                    isEroded = true;
	            }
                
                float layerOneFloorHeight = LayerHeightMapFloorHeight(index, 1);
                float layerOneHeightAboveSeaLevel = max(layerOneFloorHeight - neighborTotalTerrainAndWaterHeight, 0.0);
	            float erosionHeightLimit = (gridHydraulicErosionConfiguration.MaximalErosionHeight - min(gridHydraulicErosionConfiguration.MaximalErosionHeight, layerOneHeightAboveSeaLevel)) * 1.0 / gridHydraulicErosionConfiguration.MaximalErosionHeight;
                float sedimentCapacityTop = sedimentCapacity * erosionHeightLimit;

                if(sedimentCapacityTop > gridHydraulicErosionCell.SuspendedSediment)
	            {
		            float soilSuspendedTop = max(gridHydraulicErosionConfiguration.HorizontalSuspensionRate * (sedimentCapacityTop - gridHydraulicErosionCell.SuspendedSediment) * erosionConfiguration.TimeDelta, 0.0);

                    float suspendedSedimentTop = SuspendFromLayerOneBottom(index, soilSuspendedTop);
		            gridHydraulicErosionCell.SuspendedSediment += suspendedSedimentTop;

                    isEroded = true;
	            }
                
                gridHydraulicErosionCells[neighborIndex] = gridHydraulicErosionCell;

                return isEroded;
            }
        }
    }
    
    return false;
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute
//https://github.com/keepitwiel/hydraulic-erosion-simulator/blob/main/src/algorithm.py
//https://github.com/karhu/terrain-erosion/blob/master/Simulation/FluidSimulation.cpp
//depth limit
//https://github.com/patiltanma/15618-FinalProject/blob/master/Renderer/Renderer/erosion_kernel.cu
//https://github.com/Clocktown/CUDA-3D-Hydraulic-Erosion-Simulation-with-Layered-Stacks/blob/main/core/geo/device/erosion.cu
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
	
    if(TrySplit(index, indexLeft, vec2(-1, 0)))
    {
    }
    else if(TrySplit(index, indexRight, vec2(1, 0)))
    {
    }
    else if(TrySplit(index, indexDown, vec2(0, -1)))
    {
    }
    else if(TrySplit(index, indexUp, vec2(0, 1)))
    {
    }
    
    memoryBarrier();
}