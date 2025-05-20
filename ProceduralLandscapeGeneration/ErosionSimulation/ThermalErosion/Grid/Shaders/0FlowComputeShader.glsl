#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    uint LayerCount;
    bool AreTerrainColorsEnabled;
    bool ArePlateTectonicsPlateColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

struct ErosionConfiguration
{
    float SeaLevel;
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
    float BedrockFlowLeft;
    float BedrockFlowRight;
    float BedrockFlowUp;
    float BedrockFlowDown;
    float CoarseSedimentFlowLeft;
    float CoarseSedimentFlowRight;
    float CoarseSedimentFlowUp;
    float CoarseSedimentFlowDown;
    float FineSedimentFlowLeft;
    float FineSedimentFlowRight;
    float FineSedimentFlowUp;
    float FineSedimentFlowDown;
};

layout(std430, binding = 13) buffer gridThermalErosionCellShaderBuffer
{
    GridThermalErosionCell[] gridThermalErosionCells;
};

struct LayersConfiguration
{
    float Hardness;
    float TangensAngleOfRepose;
};

layout(std430, binding = 18) buffer layersConfigurationShaderBuffer
{
    LayersConfiguration[] layersConfiguration;
};

uint myHeightMapSideLength;
uint myHeightMapLength;

float FineSedimentHeight(uint index)
{
    return heightMap[index + (mapGenerationConfiguration.LayerCount - 1) * myHeightMapLength];
}

float CoarseSedimentHeight(uint index)
{
    return heightMap[index + 1 * myHeightMapLength];
}

float BedrockHeight(uint index)
{
    return heightMap[index];
}

float TotalCoarseSedimentHeight(uint index)
{
    float height = 0;
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount - 1; layer++)
    {
        height += heightMap[index + layer * myHeightMapLength];
    }
    return height;
}

float TotalFineSedimentHeight(uint index)
{
    float height = 0;
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        height += heightMap[index + layer * myHeightMapLength];
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
    uint id = gl_GlobalInvocationID.x;
    myHeightMapLength = heightMap.length() / mapGenerationConfiguration.LayerCount;
    if(id >= myHeightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapLength));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[id];

	float bedrockHeight = BedrockHeight(id);
	float bedrockHeightDifferenceLeft = max(bedrockHeight - BedrockHeight(GetIndex(x - 1, y)), 0.0);
	float bedrockHeightDifferenceRight = max(bedrockHeight - BedrockHeight(GetIndex(x + 1, y)), 0.0);
	float bedrockHeightDifferenceUp = max(bedrockHeight - BedrockHeight(GetIndex(x, y + 1)), 0.0);
	float bedrockHeightDifferenceDown = max(bedrockHeight - BedrockHeight(GetIndex(x, y - 1)), 0.0);
	float maxBedrockHeightDifference = max(max(bedrockHeightDifferenceLeft, bedrockHeightDifferenceRight), max(bedrockHeightDifferenceDown, bedrockHeightDifferenceUp));

	float bedrockVolumeToBeMoved = maxBedrockHeightDifference * thermalErosionConfiguration.ErosionRate;
	
	float bedrockTangensAngleLeft = bedrockHeightDifferenceLeft * mapGenerationConfiguration.HeightMultiplier / 1.0;
	float bedrockTangensAngleRight = bedrockHeightDifferenceRight * mapGenerationConfiguration.HeightMultiplier / 1.0;
	float bedrockTangensAngleUp = bedrockHeightDifferenceUp * mapGenerationConfiguration.HeightMultiplier / 1.0;
	float bedrockTangensAngleDown = bedrockHeightDifferenceDown * mapGenerationConfiguration.HeightMultiplier / 1.0;
	
	float flowLeft = 0;
	float bedrockAngleOfRepose = layersConfiguration[0].TangensAngleOfRepose;
	if (bedrockTangensAngleLeft > bedrockAngleOfRepose)
	{
		flowLeft = bedrockHeightDifferenceLeft;
	}

	float flowRight = 0;
	if (bedrockTangensAngleRight > bedrockAngleOfRepose)
	{
		flowRight = bedrockHeightDifferenceRight;
	}

	float flowUp = 0;
	if (bedrockTangensAngleUp > bedrockAngleOfRepose)
	{
		flowUp = bedrockHeightDifferenceUp;
	}

	float flowDown = 0;
	if (bedrockTangensAngleDown > bedrockAngleOfRepose)
	{
		flowDown = bedrockHeightDifferenceDown;
	}

	// Output flux
	float totalBedrockHeightDifference = bedrockHeightDifferenceLeft + bedrockHeightDifferenceRight + bedrockHeightDifferenceDown + bedrockHeightDifferenceUp;

	if(x > 0)
	{
		gridThermalErosionCell.BedrockFlowLeft = max(bedrockVolumeToBeMoved * flowLeft / totalBedrockHeightDifference * erosionConfiguration.TimeDelta, 0.0);
	}
	else
	{
		gridThermalErosionCell.BedrockFlowLeft = 0;
	}
		
	if(x < myHeightMapSideLength - 1)
	{
		gridThermalErosionCell.BedrockFlowRight = max(bedrockVolumeToBeMoved * flowRight / totalBedrockHeightDifference * erosionConfiguration.TimeDelta, 0.0);
	}
	else
	{
		gridThermalErosionCell.BedrockFlowRight = 0;
	}

	if(y > 0)
	{
		gridThermalErosionCell.BedrockFlowDown = max(bedrockVolumeToBeMoved * flowDown / totalBedrockHeightDifference * erosionConfiguration.TimeDelta, 0.0);
	}
	else
	{
		gridThermalErosionCell.BedrockFlowDown = 0;
	}
		
	if(y < myHeightMapSideLength - 1)
	{
		gridThermalErosionCell.BedrockFlowUp = max(bedrockVolumeToBeMoved * flowUp / totalBedrockHeightDifference * erosionConfiguration.TimeDelta, 0.0);
	}
	else
	{
		gridThermalErosionCell.BedrockFlowUp = 0;
	}

	if(mapGenerationConfiguration.LayerCount > 2)
	{
		float totalCoarseSedimentHeight = TotalCoarseSedimentHeight(id);
		float totalCoarseSedimentHeightDifferenceLeft = max(totalCoarseSedimentHeight - TotalCoarseSedimentHeight(GetIndex(x - 1, y)), 0.0);
		float totalCoarseSedimentHeightDifferenceRight = max(totalCoarseSedimentHeight - TotalCoarseSedimentHeight(GetIndex(x + 1, y)), 0.0);
		float totalCoarseSedimentHeightDifferenceUp = max(totalCoarseSedimentHeight - TotalCoarseSedimentHeight(GetIndex(x, y + 1)), 0.0);
		float totalCoarseSedimentHeightDifferenceDown = max(totalCoarseSedimentHeight - TotalCoarseSedimentHeight(GetIndex(x, y - 1)), 0.0);
		float maxTotalCoarseSedimentHeightDifference = max(max(totalCoarseSedimentHeightDifferenceLeft, totalCoarseSedimentHeightDifferenceRight), max(totalCoarseSedimentHeightDifferenceDown, totalCoarseSedimentHeightDifferenceUp));
		
		float sedimentVolumeToBeMoved = 0;
		if(maxTotalCoarseSedimentHeightDifference > 0)
		{
			float coarseSedimentHeight = CoarseSedimentHeight(id) / 4;
			if(maxTotalCoarseSedimentHeightDifference < coarseSedimentHeight)
			{
				sedimentVolumeToBeMoved = maxTotalCoarseSedimentHeightDifference * thermalErosionConfiguration.ErosionRate;
			}
			else
			{
				sedimentVolumeToBeMoved = coarseSedimentHeight * thermalErosionConfiguration.ErosionRate;
			}
		}
	
		float sedimentTangensAngleLeft = totalCoarseSedimentHeightDifferenceLeft * mapGenerationConfiguration.HeightMultiplier / 1.0;
		float sedimentTangensAngleRight = totalCoarseSedimentHeightDifferenceRight * mapGenerationConfiguration.HeightMultiplier / 1.0;
		float sedimentTangensAngleUp = totalCoarseSedimentHeightDifferenceUp * mapGenerationConfiguration.HeightMultiplier / 1.0;
		float sedimentTangensAngleDown = totalCoarseSedimentHeightDifferenceDown * mapGenerationConfiguration.HeightMultiplier / 1.0;
	
		float flowLeft = 0;
		float coarseSedimentAngleOfRepose = layersConfiguration[1].TangensAngleOfRepose;
		if (sedimentTangensAngleLeft > coarseSedimentAngleOfRepose)
		{
			flowLeft = totalCoarseSedimentHeightDifferenceLeft;
		}

		float flowRight = 0;
		if (sedimentTangensAngleRight > coarseSedimentAngleOfRepose)
		{
			flowRight = totalCoarseSedimentHeightDifferenceRight;
		}

		float flowDown = 0;
		if (sedimentTangensAngleDown > coarseSedimentAngleOfRepose)
		{
			flowDown = totalCoarseSedimentHeightDifferenceDown;
		}

		float flowUp = 0;
		if (sedimentTangensAngleUp > coarseSedimentAngleOfRepose)
		{
			flowUp = totalCoarseSedimentHeightDifferenceUp;
		}

		// Output flux
		float totalTotalCoarseSedimentHeightDifference = totalCoarseSedimentHeightDifferenceLeft + totalCoarseSedimentHeightDifferenceRight + totalCoarseSedimentHeightDifferenceDown + totalCoarseSedimentHeightDifferenceUp;
		if(x > 0)
		{
			gridThermalErosionCell.CoarseSedimentFlowLeft = max(sedimentVolumeToBeMoved * flowLeft / totalTotalCoarseSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.CoarseSedimentFlowLeft = 0;
		}
		
		if(x < myHeightMapSideLength - 1)
		{
			gridThermalErosionCell.CoarseSedimentFlowRight = max(sedimentVolumeToBeMoved * flowRight / totalTotalCoarseSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.CoarseSedimentFlowRight = 0;
		}

		if(y > 0)
		{
			gridThermalErosionCell.CoarseSedimentFlowDown = max(sedimentVolumeToBeMoved * flowDown / totalTotalCoarseSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.CoarseSedimentFlowDown = 0;
		}
		
		if(y < myHeightMapSideLength - 1)
		{
			gridThermalErosionCell.CoarseSedimentFlowUp = max(sedimentVolumeToBeMoved * flowUp / totalTotalCoarseSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.CoarseSedimentFlowUp = 0;
		}
	}
	
	if(mapGenerationConfiguration.LayerCount > 1)
	{
		float totalFineSedimentHeight = TotalFineSedimentHeight(id);
		float totalFineSedimentHeightDifferenceLeft = max(totalFineSedimentHeight - TotalFineSedimentHeight(GetIndex(x - 1, y)), 0.0);
		float totalFineSedimentHeightDifferenceRight = max(totalFineSedimentHeight - TotalFineSedimentHeight(GetIndex(x + 1, y)), 0.0);
		float totalFineSedimentHeightDifferenceUp = max(totalFineSedimentHeight - TotalFineSedimentHeight(GetIndex(x, y + 1)), 0.0);
		float totalFineSedimentHeightDifferenceDown = max(totalFineSedimentHeight - TotalFineSedimentHeight(GetIndex(x, y - 1)), 0.0);
		float maxTotalFineSedimentHeightDifference = max(max(totalFineSedimentHeightDifferenceLeft, totalFineSedimentHeightDifferenceRight), max(totalFineSedimentHeightDifferenceDown, totalFineSedimentHeightDifferenceUp));
		
		float sedimentVolumeToBeMoved = 0;
		if(maxTotalFineSedimentHeightDifference > 0)
		{
			float fineSedimentHeight = FineSedimentHeight(id) / 4;
			if(maxTotalFineSedimentHeightDifference < fineSedimentHeight)
			{
				sedimentVolumeToBeMoved = maxTotalFineSedimentHeightDifference * thermalErosionConfiguration.ErosionRate;
			}
			else
			{
				sedimentVolumeToBeMoved = fineSedimentHeight * thermalErosionConfiguration.ErosionRate;
			}
		}
	
		float sedimentTangensAngleLeft = totalFineSedimentHeightDifferenceLeft * mapGenerationConfiguration.HeightMultiplier / 1.0;
		float sedimentTangensAngleRight = totalFineSedimentHeightDifferenceRight * mapGenerationConfiguration.HeightMultiplier / 1.0;
		float sedimentTangensAngleUp = totalFineSedimentHeightDifferenceUp * mapGenerationConfiguration.HeightMultiplier / 1.0;
		float sedimentTangensAngleDown = totalFineSedimentHeightDifferenceDown * mapGenerationConfiguration.HeightMultiplier / 1.0;
	
		float flowLeft = 0;
		float fineSedimentAngleOfRepose = layersConfiguration[mapGenerationConfiguration.LayerCount - 1].TangensAngleOfRepose;
		if (sedimentTangensAngleLeft > fineSedimentAngleOfRepose)
		{
			flowLeft = totalFineSedimentHeightDifferenceLeft;
		}

		float flowRight = 0;
		if (sedimentTangensAngleRight > fineSedimentAngleOfRepose)
		{
			flowRight = totalFineSedimentHeightDifferenceRight;
		}

		float flowDown = 0;
		if (sedimentTangensAngleDown > fineSedimentAngleOfRepose)
		{
			flowDown = totalFineSedimentHeightDifferenceDown;
		}

		float flowUp = 0;
		if (sedimentTangensAngleUp > fineSedimentAngleOfRepose)
		{
			flowUp = totalFineSedimentHeightDifferenceUp;
		}

		// Output flux
		float totalTotalFineSedimentHeightDifference = totalFineSedimentHeightDifferenceLeft + totalFineSedimentHeightDifferenceRight + totalFineSedimentHeightDifferenceDown + totalFineSedimentHeightDifferenceUp;
		if(x > 0)
		{
			gridThermalErosionCell.FineSedimentFlowLeft = max(sedimentVolumeToBeMoved * flowLeft / totalTotalFineSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.FineSedimentFlowLeft = 0;
		}
		
		if(x < myHeightMapSideLength - 1)
		{
			gridThermalErosionCell.FineSedimentFlowRight = max(sedimentVolumeToBeMoved * flowRight / totalTotalFineSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.FineSedimentFlowRight = 0;
		}

		if(y > 0)
		{
			gridThermalErosionCell.FineSedimentFlowDown = max(sedimentVolumeToBeMoved * flowDown / totalTotalFineSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.FineSedimentFlowDown = 0;
		}
		
		if(y < myHeightMapSideLength - 1)
		{
			gridThermalErosionCell.FineSedimentFlowUp = max(sedimentVolumeToBeMoved * flowUp / totalTotalFineSedimentHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.FineSedimentFlowUp = 0;
		}
	}

	gridThermalErosionCells[id] = gridThermalErosionCell;
	
	memoryBarrier();
}