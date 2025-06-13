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

uint GetIndex(uint x, uint y)
{
    return uint((y * myHeightMapSideLength) + x);
}

float HeightMapFloorHeight(uint index, uint layer)
{
    if(layer < 1)
    {
        return 0.0;
    }
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

uint LayerHeightMapOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

float TotalSedimentHeightMapHeight(uint index, uint stopRockType)
{
    float heightMapFloorHeight = 0.0;
	float rockTypeHeight = 0.0;
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
		heightMapFloorHeight = 0.0;
        if(layer > 0)
        {
            heightMapFloorHeight = HeightMapFloorHeight(index, layer);
            if(heightMapFloorHeight == 0)
            {
                continue;
            }
        }
        for(uint rockType = 0; rockType <= stopRockType; rockType++)
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

float RockTypeSedimentAmount(uint index, uint rockType)
{
	float rockTypeHeight = 0.0;
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        rockTypeHeight += heightMap[index + rockType * myHeightMapPlaneSize + LayerHeightMapOffset(layer)];
    }
    return rockTypeHeight;
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

	for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
	{
		uint gridThermalErosionCellsIndexOffset = rockType * myHeightMapPlaneSize;
		GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[index + gridThermalErosionCellsIndexOffset];

		float totalSedimentHeight = TotalSedimentHeightMapHeight(index, rockType);
		float totalSedimentHeightDifferenceLeft = max(totalSedimentHeight - TotalSedimentHeightMapHeight(indexLeft, rockType), 0.0);
		float totalSedimentHeightDifferenceRight = max(totalSedimentHeight - TotalSedimentHeightMapHeight(indexRight, rockType), 0.0);
		float totalSedimentHeightDifferenceDown = max(totalSedimentHeight - TotalSedimentHeightMapHeight(indexDown, rockType), 0.0);
		float totalSedimentHeightDifferenceUp = max(totalSedimentHeight - TotalSedimentHeightMapHeight(indexUp, rockType), 0.0);
		float maxTotalSedimentHeightDifference = max(max(totalSedimentHeightDifferenceLeft, totalSedimentHeightDifferenceRight), max(totalSedimentHeightDifferenceDown, totalSedimentHeightDifferenceUp));
		
		float rockTypeSedimentAmount = RockTypeSedimentAmount(index, rockType);
		float sedimentVolumeToBeMoved = 0;
		if(maxTotalSedimentHeightDifference > 0)
		{
			if(maxTotalSedimentHeightDifference < rockTypeSedimentAmount)
			{
				sedimentVolumeToBeMoved = maxTotalSedimentHeightDifference * thermalErosionConfiguration.ErosionRate;
			}
			else
			{
				sedimentVolumeToBeMoved = rockTypeSedimentAmount * thermalErosionConfiguration.ErosionRate;
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
			gridThermalErosionCell.SedimentFlowLeft = max(sedimentVolumeToBeMoved * flowLeft / totalTotalSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.SedimentFlowLeft = 0;
		}
		
		if(x < myHeightMapSideLength - 1)
		{
			gridThermalErosionCell.SedimentFlowRight = max(sedimentVolumeToBeMoved * flowRight / totalTotalSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.SedimentFlowRight = 0;
		}

		if(y > 0)
		{
			gridThermalErosionCell.SedimentFlowDown = max(sedimentVolumeToBeMoved * flowDown / totalTotalSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.SedimentFlowDown = 0;
		}
		
		if(y < myHeightMapSideLength - 1)
		{
			gridThermalErosionCell.SedimentFlowUp = max(sedimentVolumeToBeMoved * flowUp / totalTotalSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.SedimentFlowUp = 0;
		}

		gridThermalErosionCells[index + gridThermalErosionCellsIndexOffset] = gridThermalErosionCell;
	}
	
	memoryBarrier();
}