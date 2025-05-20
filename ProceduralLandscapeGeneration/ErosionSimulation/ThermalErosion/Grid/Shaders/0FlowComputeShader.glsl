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
    float FlowLeft;
    float FlowRight;
    float FlowUp;
    float FlowDown;
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

float SedimentHeight(uint index)
{
    return heightMap[index + mapGenerationConfiguration.LayerCount * myHeightMapLength];
}

float BedrockHeight(uint index)
{
    return heightMap[index];
}

float TotalHeight(uint index)
{
    float height = 0;
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        height += heightMap[index + layer * myHeightMapLength];
    }
    return height;
}

uint getIndex(uint x, uint y)
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
	float bedrockHeightDifferenceLeft = max(bedrockHeight - BedrockHeight(getIndex(x - 1, y)), 0.0);
	float bedrockHeightDifferenceRight = max(bedrockHeight - BedrockHeight(getIndex(x + 1, y)), 0.0);
	float bedrockHeightDifferenceUp = max(bedrockHeight - BedrockHeight(getIndex(x, y + 1)), 0.0);
	float bedrockHeightDifferenceDown = max(bedrockHeight - BedrockHeight(getIndex(x, y - 1)), 0.0);
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
		gridThermalErosionCell.FlowLeft = max(bedrockVolumeToBeMoved * flowLeft / totalBedrockHeightDifference * erosionConfiguration.TimeDelta, 0.0);
	}
	else
	{
		gridThermalErosionCell.FlowLeft = 0;
	}
		
	if(x < myHeightMapSideLength - 1)
	{
		gridThermalErosionCell.FlowRight = max(bedrockVolumeToBeMoved * flowRight / totalBedrockHeightDifference * erosionConfiguration.TimeDelta, 0.0);
	}
	else
	{
		gridThermalErosionCell.FlowRight = 0;
	}

	if(y > 0)
	{
		gridThermalErosionCell.FlowDown = max(bedrockVolumeToBeMoved * flowDown / totalBedrockHeightDifference * erosionConfiguration.TimeDelta, 0.0);
	}
	else
	{
		gridThermalErosionCell.FlowDown = 0;
	}
		
	if(y < myHeightMapSideLength - 1)
	{
		gridThermalErosionCell.FlowUp = max(bedrockVolumeToBeMoved * flowUp / totalBedrockHeightDifference * erosionConfiguration.TimeDelta, 0.0);
	}
	else
	{
		gridThermalErosionCell.FlowUp = 0;
	}

	if(mapGenerationConfiguration.LayerCount > 1)
	{
		float totalHeight = TotalHeight(id);
		float totalHeightDifferenceLeft = max(totalHeight - TotalHeight(getIndex(x - 1, y)), 0.0);
		float totalHeightDifferenceRight = max(totalHeight - TotalHeight(getIndex(x + 1, y)), 0.0);
		float totalHeightDifferenceUp = max(totalHeight - TotalHeight(getIndex(x, y + 1)), 0.0);
		float totalHeightDifferenceDown = max(totalHeight - TotalHeight(getIndex(x, y - 1)), 0.0);
		float maxTotalHeightDifference = max(max(totalHeightDifferenceLeft, totalHeightDifferenceRight), max(totalHeightDifferenceDown, totalHeightDifferenceUp));
		
		float sedimentVolumeToBeMoved = 0;
		if(maxTotalHeightDifference > 0)
		{
			float sedimentHeight = SedimentHeight(id);
			if(maxTotalHeightDifference < sedimentHeight)
			{
				sedimentVolumeToBeMoved = maxTotalHeightDifference * thermalErosionConfiguration.ErosionRate;
			}
			else
			{
				sedimentVolumeToBeMoved = sedimentHeight * thermalErosionConfiguration.ErosionRate;
			}
		}
	
		float sedimentTangensAngleLeft = totalHeightDifferenceLeft * mapGenerationConfiguration.HeightMultiplier / 1.0;
		float sedimentTangensAngleRight = totalHeightDifferenceRight * mapGenerationConfiguration.HeightMultiplier / 1.0;
		float sedimentTangensAngleUp = totalHeightDifferenceUp * mapGenerationConfiguration.HeightMultiplier / 1.0;
		float sedimentTangensAngleDown = totalHeightDifferenceDown * mapGenerationConfiguration.HeightMultiplier / 1.0;
	
		float flowLeft = 0;
		float sedimentAngleOfRepose = layersConfiguration[1].TangensAngleOfRepose;
		if (sedimentTangensAngleLeft > sedimentAngleOfRepose)
		{
			flowLeft = totalHeightDifferenceLeft;
		}

		float flowRight = 0;
		if (sedimentTangensAngleRight > sedimentAngleOfRepose)
		{
			flowRight = totalHeightDifferenceRight;
		}

		float flowDown = 0;
		if (sedimentTangensAngleDown > sedimentAngleOfRepose)
		{
			flowDown = totalHeightDifferenceDown;
		}

		float flowUp = 0;
		if (sedimentTangensAngleUp > sedimentAngleOfRepose)
		{
			flowUp = totalHeightDifferenceUp;
		}

		// Output flux
		float totalTotalHeightDifference = totalHeightDifferenceLeft + totalHeightDifferenceRight + totalHeightDifferenceDown + totalHeightDifferenceUp;
		if(x > 0)
		{
			gridThermalErosionCell.FlowLeft += max(sedimentVolumeToBeMoved * flowLeft / totalTotalHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.FlowLeft = 0;
		}
		
		if(x < myHeightMapSideLength - 1)
		{
			gridThermalErosionCell.FlowRight += max(sedimentVolumeToBeMoved * flowRight / totalTotalHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.FlowRight = 0;
		}

		if(y > 0)
		{
			gridThermalErosionCell.FlowDown += max(sedimentVolumeToBeMoved * flowDown / totalTotalHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.FlowDown = 0;
		}
		
		if(y < myHeightMapSideLength - 1)
		{
			gridThermalErosionCell.FlowUp += max(sedimentVolumeToBeMoved * flowUp / totalTotalHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.FlowUp = 0;
		}
	}

	gridThermalErosionCells[id] = gridThermalErosionCell;
	
	memoryBarrier();
}