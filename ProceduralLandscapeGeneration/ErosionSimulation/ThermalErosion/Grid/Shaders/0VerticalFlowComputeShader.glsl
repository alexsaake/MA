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
    float SedimentFlowLeft;
    float SedimentFlowRight;
    float SedimentFlowUp;
    float SedimentFlowDown;
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

float RockTypeSedimentHeight(uint index, uint rockType)
{
    return heightMap[index + rockType * myHeightMapPlaneSize];
}

float TotalSedimentHeight(uint index, uint stopRockType)
{
    float height = 0;
    for(uint rockType = 0; rockType <= stopRockType; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapPlaneSize];
    }
    return height;
}

uint GetIndex(uint x, uint y)
{
    return uint((y * myHeightMapSideLength) + x);
}

float TotalHeightMapHeight(uint index)
{
    float height = 0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapPlaneSize];
    }
    return height;
}

float HeightMapLayerFloorHeight(uint index, uint layer)
{
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

float RemainingLayerHeight(uint index)
{
    if(mapGenerationConfiguration.LayerCount == 1)
    {
        return 1;
    }
    float heightMapLayerCeilingHeight = HeightMapLayerFloorHeight(index, 1);
    if(heightMapLayerCeilingHeight == 0)
    {
        return 1;
    }
    float totalHeightMapHeight = TotalHeightMapHeight(index);
    float remainingLayerHeight = heightMapLayerCeilingHeight - totalHeightMapHeight;
    return remainingLayerHeight;
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
	
    uint leftIndex = GetIndex(x - 1, y);
    uint rightIndex = GetIndex(x + 1, y);
    uint downIndex = GetIndex(x, y - 1);
    uint upIndex = GetIndex(x, y + 1);

	for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
	{
		uint gridThermalErosionCellsIndexOffset = rockType * myHeightMapPlaneSize;
		GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[index + gridThermalErosionCellsIndexOffset];

		float totalSedimentHeight = TotalSedimentHeight(index, rockType);
		float totalSedimentHeightDifferenceLeft = max(totalSedimentHeight - TotalSedimentHeight(leftIndex, rockType), 0.0);
		float totalSedimentHeightDifferenceRight = max(totalSedimentHeight - TotalSedimentHeight(rightIndex, rockType), 0.0);
		float totalSedimentHeightDifferenceDown = max(totalSedimentHeight - TotalSedimentHeight(downIndex, rockType), 0.0);
		float totalSedimentHeightDifferenceUp = max(totalSedimentHeight - TotalSedimentHeight(upIndex, rockType), 0.0);
		float maxTotalSedimentHeightDifference = max(max(totalSedimentHeightDifferenceLeft, totalSedimentHeightDifferenceRight), max(totalSedimentHeightDifferenceDown, totalSedimentHeightDifferenceUp));
		
		float rockTypeSedimentHeight = RockTypeSedimentHeight(index, rockType);
		float sedimentVolumeToBeMoved = 0;
		if(maxTotalSedimentHeightDifference > 0)
		{
			if(maxTotalSedimentHeightDifference < rockTypeSedimentHeight)
			{
				sedimentVolumeToBeMoved = maxTotalSedimentHeightDifference * thermalErosionConfiguration.ErosionRate;
			}
			else
			{
				sedimentVolumeToBeMoved = rockTypeSedimentHeight * thermalErosionConfiguration.ErosionRate;
			}
		}
	
		float sedimentTangensAngleLeft = totalSedimentHeightDifferenceLeft * mapGenerationConfiguration.HeightMultiplier / 1.0;
		float sedimentTangensAngleRight = totalSedimentHeightDifferenceRight * mapGenerationConfiguration.HeightMultiplier / 1.0;
		float sedimentTangensAngleUp = totalSedimentHeightDifferenceUp * mapGenerationConfiguration.HeightMultiplier / 1.0;
		float sedimentTangensAngleDown = totalSedimentHeightDifferenceDown * mapGenerationConfiguration.HeightMultiplier / 1.0;
	
		float flowLeft = 0;
		float sedimentTangensAngleOfRepose = rockTypesConfiguration[rockType].TangensAngleOfRepose;
		if (sedimentTangensAngleLeft > sedimentTangensAngleOfRepose)
		{
			flowLeft = totalSedimentHeightDifferenceLeft;
		}

		float flowRight = 0;
		if (sedimentTangensAngleRight > sedimentTangensAngleOfRepose)
		{
			flowRight = totalSedimentHeightDifferenceRight;
		}

		float flowDown = 0;
		if (sedimentTangensAngleDown > sedimentTangensAngleOfRepose)
		{
			flowDown = totalSedimentHeightDifferenceDown;
		}

		float flowUp = 0;
		if (sedimentTangensAngleUp > sedimentTangensAngleOfRepose)
		{
			flowUp = totalSedimentHeightDifferenceUp;
		}

		// Output flux
		float totalTotalSedimentHeightDifference = totalSedimentHeightDifferenceLeft + totalSedimentHeightDifferenceRight + totalSedimentHeightDifferenceDown + totalSedimentHeightDifferenceUp;
		if(x > 0)
		{
			float remainingLayerHeightLeft = RemainingLayerHeight(leftIndex);
			gridThermalErosionCell.SedimentFlowLeft = min(max(sedimentVolumeToBeMoved * flowLeft / totalTotalSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0), remainingLayerHeightLeft);
		}
		else
		{
			gridThermalErosionCell.SedimentFlowLeft = 0;
		}
		
		if(x < myHeightMapSideLength - 1)
		{
			float remainingLayerHeightRight = RemainingLayerHeight(rightIndex);
			gridThermalErosionCell.SedimentFlowRight = min(max(sedimentVolumeToBeMoved * flowRight / totalTotalSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0), remainingLayerHeightRight);
		}
		else
		{
			gridThermalErosionCell.SedimentFlowRight = 0;
		}

		if(y > 0)
		{
			float remainingLayerHeightDown = RemainingLayerHeight(downIndex);
			gridThermalErosionCell.SedimentFlowDown = min(max(sedimentVolumeToBeMoved * flowDown / totalTotalSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0), remainingLayerHeightDown);
		}
		else
		{
			gridThermalErosionCell.SedimentFlowDown = 0;
		}
		
		if(y < myHeightMapSideLength - 1)
		{
			float remainingLayerHeightUp = RemainingLayerHeight(upIndex);
			gridThermalErosionCell.SedimentFlowUp = min(max(sedimentVolumeToBeMoved * flowUp / totalTotalSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0), remainingLayerHeightUp);
		}
		else
		{
			gridThermalErosionCell.SedimentFlowUp = 0;
		}

		gridThermalErosionCells[index + gridThermalErosionCellsIndexOffset] = gridThermalErosionCell;
	}
	
	memoryBarrier();
}