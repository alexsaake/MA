#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
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

struct ThermalErosionConfiguration
{
    float ErosionRate;
};

layout(std430, binding = 10) readonly restrict buffer thermalErosionConfigurationShaderBuffer
{
    ThermalErosionConfiguration thermalErosionConfiguration;
};

struct GridThermalErosionCell
{
    float RockTypeFlowLeft;
    float RockTypeFlowRight;
    float RockTypeFlowUp;
    float RockTypeFlowDown;
};

layout(std430, binding = 13) buffer gridThermalErosionCellShaderBuffer
{
    GridThermalErosionCell[] gridThermalErosionCells;
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

float HeightMapLayerRockTypeAmount(uint index, uint layer, uint rockType)
{
	return heightMap[index + HeightMapRockTypeOffset(rockType) + HeightMapLayerOffset(layer)];
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

float HeightMapLayerRockTypeHeight(uint index, uint layer, int stopRockType)
{
    float heightMapLayerRockTypeHeight = 0.0;
    for(int rockType = 0; rockType <= stopRockType; rockType++)
    {
        heightMapLayerRockTypeHeight += heightMap[index + HeightMapRockTypeOffset(uint(rockType)) + HeightMapLayerOffset(layer)];
    }
    return heightMapLayerRockTypeHeight;
}

float TotalHeightMapLayerRockTypeHeight(uint index, uint layer, int stopRockType)
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
    return heightMapLayerFloorHeight + HeightMapLayerRockTypeHeight(index, layer, stopRockType);
}

float ReachableNeighborHeightMapHeight(uint neighborIndex, float heightMapWithoutCurrentRockTypeHeight, float heightMapWithCurrentRockTypeHeight)
{
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        float neighborTotalHeightMapLayerHeight = TotalHeightMapLayerHeight(neighborIndex, uint(layer));
        float neighborAboveHeightMapLayerFloorHeight = HeightMapLayerFloorHeight(neighborIndex, uint(layer + 1));
        if(layer > 0 && neighborTotalHeightMapLayerHeight > 0 && neighborTotalHeightMapLayerHeight < heightMapWithCurrentRockTypeHeight && (neighborAboveHeightMapLayerFloorHeight == 0 || neighborAboveHeightMapLayerFloorHeight > heightMapWithoutCurrentRockTypeHeight))
        {
            return neighborTotalHeightMapLayerHeight;
        }
        else if(layer == 0 && neighborTotalHeightMapLayerHeight >= 0 && neighborTotalHeightMapLayerHeight < heightMapWithCurrentRockTypeHeight && (neighborAboveHeightMapLayerFloorHeight == 0 || neighborAboveHeightMapLayerFloorHeight > heightMapWithoutCurrentRockTypeHeight))
        {
            return neighborTotalHeightMapLayerHeight;
        }
    }
    return 100.0;
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute

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
	    for(int rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
	    {
		    uint gridThermalErosionCellsIndexOffset = (rockType + layer * mapGenerationConfiguration.LayerCount) * myHeightMapPlaneSize;
		    GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[index + gridThermalErosionCellsIndexOffset];

		    float totalHeightMapLayerRockTypeHeight = TotalHeightMapLayerRockTypeHeight(index, layer, rockType);
		    float totalLayerHeightMapBelowRockTypeHeight = TotalHeightMapLayerRockTypeHeight(index, layer, rockType - 1);
		    float totalHeightMapLayerHeightDifferenceLeft = max(totalHeightMapLayerRockTypeHeight - ReachableNeighborHeightMapHeight(indexLeft, totalLayerHeightMapBelowRockTypeHeight, totalHeightMapLayerRockTypeHeight), 0.0);
		    float totalHeightMapLayerHeightDifferenceRight = max(totalHeightMapLayerRockTypeHeight - ReachableNeighborHeightMapHeight(indexRight, totalLayerHeightMapBelowRockTypeHeight, totalHeightMapLayerRockTypeHeight), 0.0);
		    float totalHeightMapLayerHeightDifferenceDown = max(totalHeightMapLayerRockTypeHeight - ReachableNeighborHeightMapHeight(indexDown, totalLayerHeightMapBelowRockTypeHeight, totalHeightMapLayerRockTypeHeight), 0.0);
		    float totalHeightMapLayerHeightDifferenceUp = max(totalHeightMapLayerRockTypeHeight - ReachableNeighborHeightMapHeight(indexUp, totalLayerHeightMapBelowRockTypeHeight, totalHeightMapLayerRockTypeHeight), 0.0);
		    float maxTotalHeightMapLayerHeightDifference = max(max(totalHeightMapLayerHeightDifferenceLeft, totalHeightMapLayerHeightDifferenceRight), max(totalHeightMapLayerHeightDifferenceDown, totalHeightMapLayerHeightDifferenceUp));
		
		    float heightMapLayerRockTypeAmount = HeightMapLayerRockTypeAmount(index, layer, uint(rockType));
		    float rockTypeVolumeToBeMoved = 0;
		    if(maxTotalHeightMapLayerHeightDifference > 0)
		    {
			    if(maxTotalHeightMapLayerHeightDifference < heightMapLayerRockTypeAmount)
			    {
				    rockTypeVolumeToBeMoved = maxTotalHeightMapLayerHeightDifference * thermalErosionConfiguration.ErosionRate;
			    }
			    else
			    {
				    rockTypeVolumeToBeMoved = heightMapLayerRockTypeAmount * thermalErosionConfiguration.ErosionRate;
			    }
		    }
	
		    float sedimentTangensAngleLeft = totalHeightMapLayerHeightDifferenceLeft * mapGenerationConfiguration.HeightMultiplier / 1.0;
		    float sedimentTangensAngleRight = totalHeightMapLayerHeightDifferenceRight * mapGenerationConfiguration.HeightMultiplier / 1.0;
		    float sedimentTangensAngleUp = totalHeightMapLayerHeightDifferenceUp * mapGenerationConfiguration.HeightMultiplier / 1.0;
		    float sedimentTangensAngleDown = totalHeightMapLayerHeightDifferenceDown * mapGenerationConfiguration.HeightMultiplier / 1.0;
	
		    float flowLeft = 0.0;
		    float sedimentTangensAngleOfRepose = rockTypesConfiguration[rockType].TangensAngleOfRepose;
		    if (sedimentTangensAngleLeft > sedimentTangensAngleOfRepose)
		    {
			    flowLeft = totalHeightMapLayerHeightDifferenceLeft;
		    }

		    float flowRight = 0.0;
		    if (sedimentTangensAngleRight > sedimentTangensAngleOfRepose)
		    {
			    flowRight = totalHeightMapLayerHeightDifferenceRight;
		    }

		    float flowDown = 0.0;
		    if (sedimentTangensAngleDown > sedimentTangensAngleOfRepose)
		    {
			    flowDown = totalHeightMapLayerHeightDifferenceDown;
		    }

		    float flowUp = 0.0;
		    if (sedimentTangensAngleUp > sedimentTangensAngleOfRepose)
		    {
			    flowUp = totalHeightMapLayerHeightDifferenceUp;
		    }

		    // Output flux
		    float totalTotalSedimentHeightDifference = totalHeightMapLayerHeightDifferenceLeft + totalHeightMapLayerHeightDifferenceRight + totalHeightMapLayerHeightDifferenceDown + totalHeightMapLayerHeightDifferenceUp;
		    if(x > 0)
		    {
			    gridThermalErosionCell.RockTypeFlowLeft = max(rockTypeVolumeToBeMoved * flowLeft / totalTotalSedimentHeightDifference * erosionConfiguration.DeltaTime, 0.0);
		    }
		    else
		    {
			    gridThermalErosionCell.RockTypeFlowLeft = 0;
		    }
		
		    if(x < myHeightMapSideLength - 1)
		    {
			    gridThermalErosionCell.RockTypeFlowRight = max(rockTypeVolumeToBeMoved * flowRight / totalTotalSedimentHeightDifference * erosionConfiguration.DeltaTime, 0.0);
		    }
		    else
		    {
			    gridThermalErosionCell.RockTypeFlowRight = 0;
		    }

		    if(y > 0)
		    {
			    gridThermalErosionCell.RockTypeFlowDown = max(rockTypeVolumeToBeMoved * flowDown / totalTotalSedimentHeightDifference * erosionConfiguration.DeltaTime, 0.0);
		    }
		    else
		    {
			    gridThermalErosionCell.RockTypeFlowDown = 0;
		    }
		
		    if(y < myHeightMapSideLength - 1)
		    {
			    gridThermalErosionCell.RockTypeFlowUp = max(rockTypeVolumeToBeMoved * flowUp / totalTotalSedimentHeightDifference * erosionConfiguration.DeltaTime, 0.0);
		    }
		    else
		    {
			    gridThermalErosionCell.RockTypeFlowUp = 0;
		    }

		    gridThermalErosionCells[index + gridThermalErosionCellsIndexOffset] = gridThermalErosionCell;
	    }
	}
	
	memoryBarrier();
}