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

float TotalHeightMapHeight(uint index)
{
    float heightMapLayerFloorHeight = 0.0;
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        heightMapLayerFloorHeight = 0.0;
        if(layer > 0)
        {
            heightMapLayerFloorHeight = HeightMapLayerFloorHeight(index, uint(layer));
            if(heightMapLayerFloorHeight == 0)
            {
                continue;
            }
        }
        float heightMapLayerHeight = HeightMapLayerHeight(index, uint(layer));
        if(heightMapLayerHeight > 0)
        {
            return heightMapLayerFloorHeight + heightMapLayerHeight;
        }
    }
    return 0.0;
}

void MoveRockToAboveLayer(uint index, float splitHeight)
{
    float sedimentToFill = splitHeight;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        uint currentLayerRockTypeHeightMapIndex = index + HeightMapRockTypeOffset(rockType);
        uint aboveLayerRockTypeHeightMapIndex = index + HeightMapRockTypeOffset(rockType) + (mapGenerationConfiguration.RockTypeCount + 1) * myHeightMapPlaneSize;
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

void MoveWaterAndSuspendedSedimentToAboveLayer(uint index)
{
    uint currentLayerGridHydraulicErosionCellIndex = index;
    uint aboveLayerGridHydraulicErosionCellIndex = index + myHeightMapPlaneSize;
    
    gridHydraulicErosionCells[aboveLayerGridHydraulicErosionCellIndex].WaterHeight += gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].WaterHeight;
    gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].WaterHeight = 0.0;
    gridHydraulicErosionCells[aboveLayerGridHydraulicErosionCellIndex].SuspendedSediment += gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].SuspendedSediment;
    gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellIndex].SuspendedSediment = 0.0;
}

void SetHeightMapLayerFloorHeight(uint index, uint layer, float value)
{
    if(layer < 1
        || layer >= mapGenerationConfiguration.LayerCount)
    {
        return;
    }
    heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize] = value;
}

float SuspendFromLayerZeroTop(uint index, float requiredSediment)
{
    float suspendedSediment = 0;
    for(int rockType = int(mapGenerationConfiguration.RockTypeCount) - 1; rockType >= 0; rockType--)
    {
        uint rockTypeIndex = index + HeightMapRockTypeOffset(uint(rockType));
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

void SplitAt(uint index, float splitHeight)
{
    if(HeightMapLayerFloorHeight(index, 1) > 0)
    {
        return;
    }
    SetHeightMapLayerFloorHeight(index, 1, splitHeight);
    MoveRockToAboveLayer(index, splitHeight);
    MoveWaterAndSuspendedSedimentToAboveLayer(index);
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
                float layerZeroHeight = HeightMapLayerHeight(index, 0);
                float layerZeroHeightBelowSeaLevel = max(neighborTotalTerrainAndWaterHeight - layerZeroHeight, 0.0);
	            float erosionDepthLimit = (gridHydraulicErosionConfiguration.MaximalErosionDepth - min(gridHydraulicErosionConfiguration.MaximalErosionDepth, layerZeroHeightBelowSeaLevel)) / gridHydraulicErosionConfiguration.MaximalErosionDepth;

                float sedimentCapacity = min(gridHydraulicErosionConfiguration.SedimentCapacity * length(gridHydraulicErosionCell.WaterVelocity) * erosionDepthLimit, 1.0);
            
                if(sedimentCapacity > gridHydraulicErosionCell.SuspendedSediment)
	            {
		            float soilSuspendedBottom = max(gridHydraulicErosionConfiguration.HorizontalSuspensionRate * (sedimentCapacity - gridHydraulicErosionCell.SuspendedSediment) * erosionConfiguration.DeltaTime, 0.0);

		            SplitAt(index, neighborTotalHeight);
		            float suspendedSediment = SuspendFromLayerZeroTop(index, soilSuspendedBottom);
		            gridHydraulicErosionCell.SuspendedSediment += suspendedSediment;
	            }
                
                gridHydraulicErosionCells[neighborIndex] = gridHydraulicErosionCell;

                return true;
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
	
    bool isSplit = false;
    if(x > 0)
    {
        isSplit = TrySplit(index, indexLeft, vec2(-1, 0));
    }
    if(!isSplit
        && x < myHeightMapSideLength - 1)
    {
        isSplit = TrySplit(index, indexRight, vec2(1, 0));
    }
    if(!isSplit
        && y > 0)
    {
        isSplit = TrySplit(index, indexDown, vec2(0, -1));
    }
    if(!isSplit
        && y < myHeightMapSideLength - 1)
    {
        isSplit = TrySplit(index, indexUp, vec2(0, 1));
    }
    
    memoryBarrier();
}