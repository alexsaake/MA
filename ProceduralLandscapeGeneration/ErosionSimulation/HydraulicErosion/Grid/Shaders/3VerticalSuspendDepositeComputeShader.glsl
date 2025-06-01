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

float TotalHeightMapLayerHeight(uint index)
{
    float height = 0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapPlaneSize];
    }
    return height;
}

uint GetIndex(uint x, uint y)
{
    return uint((y * myHeightMapSideLength) + x);
}

float SuspendFromTop(uint index, float requiredSediment)
{
    float suspendedSediment = 0;
    for(int rockType = int(mapGenerationConfiguration.RockTypeCount) - 1; rockType >= 0; rockType--)
    {
        uint offsetIndex = index + rockType * myHeightMapPlaneSize;
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

float RemainingLayerHeight(uint index)
{
    float heightMapLayerCeilingHeight = HeightMapLayerFloorHeight(index, 1);
    if(heightMapLayerCeilingHeight == 0)
    {
        return 1.0;
    }
    float totalHeightMapLayerHeight = TotalHeightMapLayerHeight(index);
    float remainingLayerHeight = heightMapLayerCeilingHeight - totalHeightMapLayerHeight;
    return remainingLayerHeight;
}

void DepositeOnTop(uint index, float sediment)
{
    heightMap[index + (mapGenerationConfiguration.RockTypeCount - 1) * myHeightMapPlaneSize] += sediment;
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
    
    uint indexLeft = GetIndex(x - 1, y);
    uint indexRight = GetIndex(x + 1, y);
    uint indexDown = GetIndex(x, y - 1);
    uint indexUp = GetIndex(x, y + 1);

    GridHydraulicErosionCell gridHydraulicErosionCell = gridHydraulicErosionCells[index];

    float height = TotalHeightMapLayerHeight(index);
    float heightLeft;
    float heightRight;
    float heightDown;
    float heightUp;
    if(x > 0)
    {
        heightLeft = TotalHeightMapLayerHeight(indexLeft);
    }
    else
    {
        heightLeft = height;
    }
    if(x < myHeightMapSideLength - 1)
    {
        heightRight = TotalHeightMapLayerHeight(indexRight);
    }
    else
    {
        heightRight = height;
    }
    if(y > 0)
    {
        heightDown = TotalHeightMapLayerHeight(indexDown);
    }
    else
    {
        heightDown = height;
    }
    if(y < myHeightMapSideLength - 1)
    {
        heightUp = TotalHeightMapLayerHeight(indexUp);
    }
    else
    {
        heightUp = height;
    }

	vec3 dhdx = vec3(1.0, 0.0, (heightRight - heightLeft) / 2.0 * mapGenerationConfiguration.HeightMultiplier);
	vec3 dhdy = vec3(0.0, 1.0, (heightUp - heightDown) / 2.0 * mapGenerationConfiguration.HeightMultiplier);
	vec3 normal = normalize(cross(dhdx, dhdy));
    
    float dotProd = dot(normal, vec3(0.0, 0.0, 1.0));
    float alpha = acos(dotProd);
    float tiltAngle = sin(alpha);
	
	float erosionDepthLimit = (gridHydraulicErosionConfiguration.MaximalErosionDepth - min(gridHydraulicErosionConfiguration.MaximalErosionDepth, gridHydraulicErosionCell.WaterHeight)) / gridHydraulicErosionConfiguration.MaximalErosionDepth;
	float sedimentCapacity = gridHydraulicErosionConfiguration.SedimentCapacity * tiltAngle * length(gridHydraulicErosionCell.WaterVelocity) * erosionDepthLimit;

	if (sedimentCapacity > gridHydraulicErosionCell.SuspendedSediment)
	{
		float soilSuspended = max(gridHydraulicErosionConfiguration.SuspensionRate * (sedimentCapacity - gridHydraulicErosionCell.SuspendedSediment) * erosionConfiguration.TimeDelta, 0.0);
		float suspendedSediment = SuspendFromTop(index, soilSuspended);
		gridHydraulicErosionCell.SuspendedSediment += suspendedSediment;
	}
	else if (sedimentCapacity < gridHydraulicErosionCell.SuspendedSediment)
	{
        float remainingLayerHeight = RemainingLayerHeight(index);
		float soilDeposited = min(min(gridHydraulicErosionConfiguration.DepositionRate * (gridHydraulicErosionCell.SuspendedSediment - sedimentCapacity) * erosionConfiguration.TimeDelta, gridHydraulicErosionCell.SuspendedSediment), remainingLayerHeight);
		DepositeOnTop(index, soilDeposited);
		gridHydraulicErosionCell.SuspendedSediment -= soilDeposited;
	}
	
    gridHydraulicErosionCells[index] = gridHydraulicErosionCell;
    
    memoryBarrier();
}