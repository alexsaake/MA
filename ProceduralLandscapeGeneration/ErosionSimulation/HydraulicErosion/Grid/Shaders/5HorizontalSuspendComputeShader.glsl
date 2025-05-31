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

struct ErosionConfiguration
{
    float TimeDelta;
	bool IsWaterKeptInBoundaries;
};

layout(std430, binding = 6) readonly restrict buffer erosionConfigurationShaderBuffer
{
    ErosionConfiguration erosionConfiguration;
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

uint GetIndex(uint x, uint y)
{
    return uint((y * myHeightMapSideLength) + x);
}

void SplitAt(uint index, uint layer, float relativeHeight)
{
    uint aboveLayerFloorIndex = index + (layer + 1) * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize;
    if(heightMap[aboveLayerFloorIndex] > 0)
    {
        return;
    }
    float splitHeight = relativeHeight;
    if(layer > 0)
    {
    }
    heightMap[aboveLayerFloorIndex] = splitHeight;
    float sedimentToFill = relativeHeight;
    for(int rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        uint currentLayerRockTypeHeightMapIndex = index + rockType * myHeightMapPlaneSize + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
        uint aboveLayerRockTypeHeightMapIndex = index + rockType * myHeightMapPlaneSize + ((layer + 1) * mapGenerationConfiguration.RockTypeCount + (layer + 1)) * myHeightMapPlaneSize;
        float currentLayerRockTypeHeight = heightMap[currentLayerRockTypeHeightMapIndex];
        sedimentToFill -= currentLayerRockTypeHeight;
        if(sedimentToFill < 0)
        {
            heightMap[aboveLayerRockTypeHeightMapIndex] -= sedimentToFill;
            heightMap[currentLayerRockTypeHeightMapIndex] += sedimentToFill;
            sedimentToFill = 0;
        }
    }
}

float SuspendFromTop(uint index, uint layer, float requiredSediment)
{
    float suspendedSediment = 0;
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
    return suspendedSediment;
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute
//https://github.com/keepitwiel/hydraulic-erosion-simulator/blob/main/src/algorithm.py
//https://github.com/karhu/terrain-erosion/blob/master/Simulation/FluidSimulation.cpp
//depth limit
//https://github.com/patiltanma/15618-FinalProject/blob/master/Renderer/Renderer/erosion_kernel.cu
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
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount - 1; layer++)
    {
        uint gridHydraulicErosionCellIndexOffset = layer * myHeightMapPlaneSize;
        GridHydraulicErosionCell gridHydraulicErosionCell = gridHydraulicErosionCells[index + gridHydraulicErosionCellIndexOffset];

        float height = TotalHeightMapLayerHeight(index, layer);
        float heightLeft;
        float heightRight;
        float heightDown;
        float heightUp;
        if(x > 0)
        {
            heightLeft = TotalHeightMapLayerHeight(leftIndex, layer);
        }
        else
        {
            heightLeft = height;
        }
        if(x < myHeightMapSideLength - 1)
        {
            heightRight = TotalHeightMapLayerHeight(rightIndex, layer);
        }
        else
        {
            heightRight = height;
        }
        if(y > 0)
        {
            heightDown = TotalHeightMapLayerHeight(downIndex, layer);
        }
        else
        {
            heightDown = height;
        }
        if(y < myHeightMapSideLength - 1)
        {
            heightUp = TotalHeightMapLayerHeight(upIndex, layer);
        }
        else
        {
            heightUp = height;
        }
	
	    float erosionDepthLimit = (gridHydraulicErosionConfiguration.MaximalErosionDepth - min(gridHydraulicErosionConfiguration.MaximalErosionDepth, gridHydraulicErosionCell.WaterHeight)) / gridHydraulicErosionConfiguration.MaximalErosionDepth;
	    float sedimentCapacity = gridHydraulicErosionConfiguration.SedimentCapacity * length(gridHydraulicErosionCell.WaterVelocity) * erosionDepthLimit;

	    if (sedimentCapacity > gridHydraulicErosionCell.SuspendedSediment)
	    {
		    float soilSuspended = max(gridHydraulicErosionConfiguration.SuspensionRate * (sedimentCapacity - gridHydraulicErosionCell.SuspendedSediment) * erosionConfiguration.TimeDelta, 0.0);

            //erode to sides
            float waterHeight = gridHydraulicErosionCell.WaterHeight;
            if(waterHeight > 0)
            {
                float heightAndWaterHeight = height + waterHeight;
                if(heightAndWaterHeight < heightLeft)
                {
		            SplitAt(leftIndex, layer, heightAndWaterHeight);
		            float suspendedSediment = SuspendFromTop(leftIndex, layer, soilSuspended);
		            gridHydraulicErosionCell.SuspendedSediment += suspendedSediment;
                }
                if(heightAndWaterHeight < heightRight)
                {
		            SplitAt(rightIndex, layer, heightAndWaterHeight);
		            float suspendedSediment = SuspendFromTop(rightIndex, layer, soilSuspended);
		            gridHydraulicErosionCell.SuspendedSediment += suspendedSediment;
                }
                if(heightAndWaterHeight < heightDown)
                {
		            SplitAt(downIndex, layer, heightAndWaterHeight);
		            float suspendedSediment = SuspendFromTop(downIndex, layer, soilSuspended);
		            gridHydraulicErosionCell.SuspendedSediment += suspendedSediment;
                }
                if(heightAndWaterHeight < heightUp)
                {
		            SplitAt(upIndex, layer, heightAndWaterHeight);
		            float suspendedSediment = SuspendFromTop(upIndex, layer, soilSuspended);
		            gridHydraulicErosionCell.SuspendedSediment += suspendedSediment;
                }
            }
	    }
	
        gridHydraulicErosionCells[index + gridHydraulicErosionCellIndexOffset] = gridHydraulicErosionCell;
    }
    
    memoryBarrier();
}