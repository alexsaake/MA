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
};

layout(std430, binding = 18) buffer rockTypesConfigurationShaderBuffer
{
    RockTypeConfiguration[] rockTypesConfiguration;
};

uint myHeightMapSideLength;
uint myHeightMapLength;

float RockTypeSedimentHeight(uint index, uint layer, uint rockType)
{
    return heightMap[index + rockType * myHeightMapLength + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapLength];
}

float TotalSedimentHeight(uint index, uint stopRockType, uint layer)
{
    float height = 0;
    for(uint rockType = 0; rockType <= stopRockType; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapLength + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapLength];
    }
    return height;
}

uint GetIndex(uint x, uint y)
{
    return uint((y * myHeightMapSideLength) + x);
}

float HeightMapLayerFloorHeight(uint index, uint layer)
{
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapLength];
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
        height += heightMap[index + rockType * myHeightMapLength + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapLength];
    }
    return height;
}

float RemainingLayerHeight(uint index, uint layer)
{
    if(layer >= mapGenerationConfiguration.LayerCount - 1)
    {
        return 1.0;
    }
    float heightMapLayerCeilingHeight = HeightMapLayerFloorHeight(index, layer + 1);
    float totalHeightMapLayerHeight = TotalHeightMapLayerHeight(index, layer);
    float remainingLayerHeight = heightMapLayerCeilingHeight - totalHeightMapLayerHeight;
    if(heightMapLayerCeilingHeight == 0)
    {
        return 1.0;
    }
    return remainingLayerHeight;
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute

void main()
{	
    uint index = gl_GlobalInvocationID.x;
    myHeightMapLength = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= myHeightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapLength));

    uint x = index % myHeightMapSideLength;
    uint y = index / myHeightMapSideLength;
	
    uint leftIndex = GetIndex(x - 1, y);
    uint rightIndex = GetIndex(x + 1, y);
    uint downIndex = GetIndex(x, y - 1);
    uint upIndex = GetIndex(x, y + 1);
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
	{
		for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
		{
			uint indexOffset = rockType * myHeightMapLength + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapLength;
			GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[index + indexOffset];

			float totalSedimentHeight = TotalSedimentHeight(index, rockType, layer);
			float totalSedimentHeightDifferenceLeft = max(totalSedimentHeight - TotalSedimentHeight(leftIndex, rockType, layer), 0.0);
			float totalSedimentHeightDifferenceRight = max(totalSedimentHeight - TotalSedimentHeight(rightIndex, rockType, layer), 0.0);
			float totalSedimentHeightDifferenceDown = max(totalSedimentHeight - TotalSedimentHeight(downIndex, rockType, layer), 0.0);
			float totalSedimentHeightDifferenceUp = max(totalSedimentHeight - TotalSedimentHeight(upIndex, rockType, layer), 0.0);
			float maxTotalSedimentHeightDifference = max(max(totalSedimentHeightDifferenceLeft, totalSedimentHeightDifferenceRight), max(totalSedimentHeightDifferenceDown, totalSedimentHeightDifferenceUp));
		
			float rockTypeSedimentHeight = RockTypeSedimentHeight(index, layer, rockType);
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
				float remainingLayerHeightLeft = RemainingLayerHeight(leftIndex, layer);
				gridThermalErosionCell.SedimentFlowLeft = min(max(sedimentVolumeToBeMoved * flowLeft / totalTotalSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0), remainingLayerHeightLeft);
			}
			else
			{
				gridThermalErosionCell.SedimentFlowLeft = 0;
			}
		
			if(x < myHeightMapSideLength - 1)
			{
				float remainingLayerHeightRight = RemainingLayerHeight(rightIndex, layer);
				gridThermalErosionCell.SedimentFlowRight = min(max(sedimentVolumeToBeMoved * flowRight / totalTotalSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0), remainingLayerHeightRight);
			}
			else
			{
				gridThermalErosionCell.SedimentFlowRight = 0;
			}

			if(y > 0)
			{
				float remainingLayerHeightDown = RemainingLayerHeight(downIndex, layer);
				gridThermalErosionCell.SedimentFlowDown = min(max(sedimentVolumeToBeMoved * flowDown / totalTotalSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0), remainingLayerHeightDown);
			}
			else
			{
				gridThermalErosionCell.SedimentFlowDown = 0;
			}
		
			if(y < myHeightMapSideLength - 1)
			{
				float remainingLayerHeightUp = RemainingLayerHeight(upIndex, layer);
				gridThermalErosionCell.SedimentFlowUp = min(max(sedimentVolumeToBeMoved * flowUp / totalTotalSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0), remainingLayerHeightUp);
			}
			else
			{
				gridThermalErosionCell.SedimentFlowUp = 0;
			}

			gridThermalErosionCells[index + indexOffset] = gridThermalErosionCell;
		}
	}
	
	memoryBarrier();
}