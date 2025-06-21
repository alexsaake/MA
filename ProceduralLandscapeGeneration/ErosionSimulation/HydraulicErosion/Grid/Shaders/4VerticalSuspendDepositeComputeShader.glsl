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

float SuspendFromLayerTop(uint index, uint layer, float requiredSediment)
{
    float suspendedSediment = 0;
    if(HeightMapLayerFloorHeight(index, layer) == 0
        && layer > 0)
    {
        return 0.0;
    }
    for(int rockType = int(mapGenerationConfiguration.RockTypeCount) - 1; rockType >= 0; rockType--)
    {
        uint offsetIndex = index + HeightMapRockTypeOffset(uint(rockType)) + HeightMapLayerOffset(layer);
        float height = heightMap[offsetIndex];
        float hardness = (1.0 - rockTypesConfiguration[rockType].Hardness);
        float toBeSuspendedSediment = requiredSediment * hardness;
        if(height >= toBeSuspendedSediment)
        {
            heightMap[offsetIndex] -= toBeSuspendedSediment;
            suspendedSediment += toBeSuspendedSediment;
            break;
        }
        else if(height > 0)
        {
            float toBeSuspendedHeight = height;
            heightMap[offsetIndex] = 0;
            requiredSediment -= toBeSuspendedHeight;
            suspendedSediment += toBeSuspendedHeight;
        }
    }
    return suspendedSediment;
}

void AddHeightMapLayerFloorHeight(uint index, uint layer, float value)
{
    heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize] += value;
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

float SuspendFromLayerBottom(uint index, uint layer, float requiredSediment)
{
    if(layer < 1
        || (HeightMapLayerFloorHeight(index, layer) == 0
            && layer > 0))
    {
        return 0.0;
    }
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        uint rockTypeIndex = index + HeightMapRockTypeOffset(rockType) + HeightMapLayerOffset(layer);
        float height = heightMap[rockTypeIndex];
        float hardness = (1.0 - rockTypesConfiguration[rockType].Hardness);
        float toBeSuspendedSediment = requiredSediment * hardness;
        if(height >= toBeSuspendedSediment)
        {
            heightMap[rockTypeIndex] -= toBeSuspendedSediment;
            AddHeightMapLayerFloorHeight(index, layer, toBeSuspendedSediment);
            return toBeSuspendedSediment;
        }
    }
    return 0.0;
}

void DepositeOnLayerTop(uint index, uint startLayer, float sediment)
{
    for(int layer = int(startLayer); layer >= 0; layer--)
    {
        if(layer > 0
            && HeightMapLayerFloorHeight(index, layer) == 0)
        {
            continue;
        }
        heightMap[index + (mapGenerationConfiguration.RockTypeCount - 1) * myHeightMapPlaneSize + HeightMapLayerOffset(layer)] += sediment;
        return;
    }
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
    
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        if(layer > 0
            && HeightMapLayerFloorHeight(index, layer) == 0)
        {
            continue;
        }

        uint layerIndex = index + LayerHydraulicErosionCellsOffset(layer);
        GridHydraulicErosionCell gridHydraulicErosionCell = gridHydraulicErosionCells[layerIndex];

        float totalHeightMapHeight = TotalHeightMapHeight(index);
        float heightLeft;
        float heightRight;
        float heightDown;
        float heightUp;
        if(x > 0)
        {
            heightLeft = TotalHeightMapHeight(indexLeft);
        }
        else
        {
            heightLeft = totalHeightMapHeight;
        }
        if(x < myHeightMapSideLength - 1)
        {
            heightRight = TotalHeightMapHeight(indexRight);
        }
        else
        {
            heightRight = totalHeightMapHeight;
        }
        if(y > 0)
        {
            heightDown = TotalHeightMapHeight(indexDown);
        }
        else
        {
            heightDown = totalHeightMapHeight;
        }
        if(y < myHeightMapSideLength - 1)
        {
            heightUp = TotalHeightMapHeight(indexUp);
        }
        else
        {
            heightUp = totalHeightMapHeight;
        }

	    vec3 dhdx = vec3(1.0, 0.0, (heightRight - heightLeft) / 2.0 * mapGenerationConfiguration.HeightMultiplier);
	    vec3 dhdy = vec3(0.0, 1.0, (heightUp - heightDown) / 2.0 * mapGenerationConfiguration.HeightMultiplier);
	    vec3 normal = normalize(cross(dhdx, dhdy));
    
        float dotProd = dot(normal, vec3(0.0, 0.0, 1.0));
        float alpha = acos(dotProd);
        float tiltAngle = sin(alpha);

	    float sedimentCapacity = min(gridHydraulicErosionConfiguration.SedimentCapacity * max(tiltAngle, 0.1) * length(gridHydraulicErosionCell.WaterVelocity), 1.0);
        
        float waterHeight = gridHydraulicErosionCell.WaterHeight;

	    if (sedimentCapacity > gridHydraulicErosionCell.SuspendedSediment)
	    {
	        float erosionDepthLimit = (gridHydraulicErosionConfiguration.MaximalErosionDepth - min(gridHydraulicErosionConfiguration.MaximalErosionDepth, waterHeight)) / gridHydraulicErosionConfiguration.MaximalErosionDepth;
            float sedimentCapacityBottom = sedimentCapacity * erosionDepthLimit;
            
            if(sedimentCapacityBottom > gridHydraulicErosionCell.SuspendedSediment)
	        {
		        float soilSuspendedBottom = max(gridHydraulicErosionConfiguration.VerticalSuspensionRate * (sedimentCapacityBottom - gridHydraulicErosionCell.SuspendedSediment) * erosionConfiguration.DeltaTime, 0.0);
                float suspendedSedimentBottom = SuspendFromLayerTop(index, layer, soilSuspendedBottom);
		        gridHydraulicErosionCell.SuspendedSediment += suspendedSedimentBottom;
	        }
            
            if(mapGenerationConfiguration.LayerCount > 1
                && layer == 0)
            {
                float aboveLayerFloorHeight = HeightMapLayerFloorHeight(index, layer + 1);
                float totalLayerHeightMapAndWaterHeight = TotalHeightMapLayerHeight(index, layer) + waterHeight;
                float aboveLayerHeightAboveWater = max(aboveLayerFloorHeight - totalLayerHeightMapAndWaterHeight, 0.0);
	            float erosionHeightLimit = (gridHydraulicErosionConfiguration.MaximalErosionHeight - min(gridHydraulicErosionConfiguration.MaximalErosionHeight, aboveLayerHeightAboveWater)) / gridHydraulicErosionConfiguration.MaximalErosionHeight;
                float sedimentCapacityTop = sedimentCapacity * erosionHeightLimit;

                if(sedimentCapacityTop > gridHydraulicErosionCell.SuspendedSediment)
	            {
		            float soilSuspendedTop = max(gridHydraulicErosionConfiguration.HorizontalSuspensionRate * (sedimentCapacityTop - gridHydraulicErosionCell.SuspendedSediment) * erosionConfiguration.DeltaTime, 0.0);
                    float suspendedSedimentTop = SuspendFromLayerBottom(index, layer + 1, soilSuspendedTop);
		            gridHydraulicErosionCell.SuspendedSediment += suspendedSedimentTop;
	            }
            }
	    }
	    else if (sedimentCapacity < gridHydraulicErosionCell.SuspendedSediment)
	    {
		    float soilDeposited = 0.0;
            if(waterHeight == 0)
            {
                soilDeposited = gridHydraulicErosionCell.SuspendedSediment;
            }
            else
            {
                soilDeposited = min(gridHydraulicErosionConfiguration.DepositionRate * (gridHydraulicErosionCell.SuspendedSediment - sedimentCapacity) * erosionConfiguration.DeltaTime, gridHydraulicErosionCell.SuspendedSediment);
            }
		    DepositeOnLayerTop(index, layer, soilDeposited);
		    gridHydraulicErosionCell.SuspendedSediment -= soilDeposited;
	    }
	
        gridHydraulicErosionCells[layerIndex] = gridHydraulicErosionCell;
    }
    
    memoryBarrier();
}