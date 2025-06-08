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

float TotalLayerZeroHeightMapHeight(uint index)
{
    float height = 0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapPlaneSize];
    }
    return height;
}

void MoveSedimentToLayerOne(uint index, float splitHeight)
{    
    uint currentLayerGridHydraulicErosionCellsIndex = index + myHeightMapPlaneSize;
    uint aboveLayerGridHydraulicErosionCellsIndex = index + (1) * myHeightMapPlaneSize;
    gridHydraulicErosionCells[aboveLayerGridHydraulicErosionCellsIndex].WaterHeight += gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellsIndex].WaterHeight;
    gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellsIndex].WaterHeight = 0;
    gridHydraulicErosionCells[aboveLayerGridHydraulicErosionCellsIndex].SuspendedSediment += gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellsIndex].SuspendedSediment;
    gridHydraulicErosionCells[currentLayerGridHydraulicErosionCellsIndex].SuspendedSediment = 0;

    float sedimentToFill = splitHeight;
    for(int rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        uint currentLayerRockTypeHeightMapIndex = index + rockType * myHeightMapPlaneSize + (mapGenerationConfiguration.RockTypeCount) * myHeightMapPlaneSize;
        uint aboveLayerRockTypeHeightMapIndex = index + rockType * myHeightMapPlaneSize + ((1) * mapGenerationConfiguration.RockTypeCount + (1)) * myHeightMapPlaneSize;
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

float HeightMapFloorHeight(uint index, uint layer)
{
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

void SetHeightMapFloorHeight(uint index, uint layer, float value)
{
    heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize] = value;
}

float SuspendFromTop(uint index, float requiredSediment)
{
    float suspendedSediment = 0;
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        if(HeightMapFloorHeight(index, layer) == 0)
        {
            continue;
        }
        for(int rockType = int(mapGenerationConfiguration.RockTypeCount) - 1; rockType >= 0; rockType--)
        {
            uint offsetIndex = index + rockType * myHeightMapPlaneSize + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
            float height = heightMap[offsetIndex];
            float hardness = (1.0 - rockTypesConfiguration[rockType].Hardness);
            float toBeSuspendedSediment = requiredSediment * hardness;
            if(height >= toBeSuspendedSediment)
            {
                heightMap[offsetIndex] -= toBeSuspendedSediment;
                suspendedSediment += toBeSuspendedSediment;
                break;
            }
            else
            {
                heightMap[offsetIndex] = 0;
                requiredSediment -= height * hardness;
                suspendedSediment += height * hardness;
            }
        }
    }
    return suspendedSediment;
}

void SplitAt(uint index, float splitHeight)
{
    if(HeightMapFloorHeight(index, 1) > 0)
    {
        return;
    }
    SetHeightMapFloorHeight(index, 1, splitHeight);
    MoveSedimentToLayerOne(index, splitHeight);
}

bool TrySplit(uint index, float height, uint neighborIndex, float neighborHeight)
{
    if(HeightMapFloorHeight(index, 1) > 0)
    {
        return false;
    }
    GridHydraulicErosionCell gridHydraulicErosionCell = gridHydraulicErosionCells[neighborIndex];

    float sedimentCapacity = gridHydraulicErosionConfiguration.SedimentCapacity * length(gridHydraulicErosionCell.WaterVelocity);

	if (sedimentCapacity > gridHydraulicErosionCell.SuspendedSediment)
	{
		float soilSuspended = max(gridHydraulicErosionConfiguration.SuspensionRate * (sedimentCapacity - gridHydraulicErosionCell.SuspendedSediment) * erosionConfiguration.TimeDelta, 0.0);

        if(gridHydraulicErosionCell.WaterHeight > 0)
        {
            float neighborTotalHeight = neighborHeight + gridHydraulicErosionCell.WaterHeight;
            if(neighborTotalHeight < height)
            {
		        SplitAt(index, neighborTotalHeight);
		        float suspendedSediment = SuspendFromTop(index, soilSuspended);
		        gridHydraulicErosionCell.SuspendedSediment += suspendedSediment;
                
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

    float height = TotalLayerZeroHeightMapHeight(index);
    float heightLeft;
    float heightRight;
    float heightDown;
    float heightUp;
    if(x > 0)
    {
        heightLeft = TotalLayerZeroHeightMapHeight(indexLeft);
    }
    else
    {
        heightLeft = height;
    }
    if(x < myHeightMapSideLength - 1)
    {
        heightRight = TotalLayerZeroHeightMapHeight(indexRight);
    }
    else
    {
        heightRight = height;
    }
    if(y > 0)
    {
        heightDown = TotalLayerZeroHeightMapHeight(indexDown);
    }
    else
    {
        heightDown = height;
    }
    if(y < myHeightMapSideLength - 1)
    {
        heightUp = TotalLayerZeroHeightMapHeight(indexUp);
    }
    else
    {
        heightUp = height;
    }
	
    if(TrySplit(index, height, indexLeft, heightLeft))
    {
    }
    else if(TrySplit(index, height, indexRight, heightRight))
    {
    }
    else if(TrySplit(index, height, indexDown, heightDown))
    {
    }
    else if(TrySplit(index, height, indexUp, heightUp))
    {
    }
    
    memoryBarrier();
}