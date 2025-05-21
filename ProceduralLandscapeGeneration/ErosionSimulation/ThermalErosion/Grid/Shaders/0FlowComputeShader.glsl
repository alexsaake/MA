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

float RockTypeSedimentHeight(uint index, uint rockType)
{
    return heightMap[index + rockType * myHeightMapLength];
}

float TotalSedimentHeight(uint index, uint stopRockType)
{
    float height = 0;
    for(uint rockType = 0; rockType <= stopRockType; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapLength];
    }
    return height;
}

uint GetIndex(uint x, uint y)
{
    return uint((y * myHeightMapSideLength) + x);
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
	
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
	{
        uint indexOffset = rockType * myHeightMapLength;
		GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[index + indexOffset];

		float totalSedimentHeight = TotalSedimentHeight(index, rockType);
		float totalSedimentHeightDifferenceLeft = max(totalSedimentHeight - TotalSedimentHeight(GetIndex(x - 1, y), rockType), 0.0);
		float totalSedimentHeightDifferenceRight = max(totalSedimentHeight - TotalSedimentHeight(GetIndex(x + 1, y), rockType), 0.0);
		float totalSedimentHeightDifferenceUp = max(totalSedimentHeight - TotalSedimentHeight(GetIndex(x, y + 1), rockType), 0.0);
		float totalSedimentHeightDifferenceDown = max(totalSedimentHeight - TotalSedimentHeight(GetIndex(x, y - 1), rockType), 0.0);
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

		gridThermalErosionCells[index + indexOffset] = gridThermalErosionCell;
	}
	
	memoryBarrier();
}