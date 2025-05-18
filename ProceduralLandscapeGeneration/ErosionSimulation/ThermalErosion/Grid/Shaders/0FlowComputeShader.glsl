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
    float Dampening;
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
    float TangensTalusAngle;
};

layout(std430, binding = 18) buffer layersConfigurationShaderBuffer
{
    LayersConfiguration[] layersConfiguration;
};

uint myHeightMapSideLength;
uint myHeightMapLength;

float TangensTalusAngle(uint index)
{
	for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
	{
		if(heightMap[index + layer * myHeightMapLength] > 0)
		{
			return layersConfiguration[layer].TangensTalusAngle;
		}
	}
	return layersConfiguration[0].TangensTalusAngle;
}

float totalHeight(uint index)
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

	float heightDifferenceLeft = max(totalHeight(id) - totalHeight(getIndex(x - 1, y)), 0.0);
	float heightDifferenceRight = max(totalHeight(id) - totalHeight(getIndex(x + 1, y)), 0.0);
	float heightDifferenceTop = max(totalHeight(id) - totalHeight(getIndex(x, y + 1)), 0.0);
	float heightDifferenceBottom = max(totalHeight(id) - totalHeight(getIndex(x, y - 1)), 0.0);
	float maxHeightDifference = max(max(heightDifferenceLeft, heightDifferenceRight), max(heightDifferenceBottom, heightDifferenceTop));

	float volumeToBeMoved = maxHeightDifference * thermalErosionConfiguration.ErosionRate * (1.0 - thermalErosionConfiguration.Dampening);
	
	float tangensAngleLeft = heightDifferenceLeft * mapGenerationConfiguration.HeightMultiplier / 1.0;
	float tangensAngleRight = heightDifferenceRight * mapGenerationConfiguration.HeightMultiplier / 1.0;
	float tangensAngleTop = heightDifferenceTop * mapGenerationConfiguration.HeightMultiplier / 1.0;
	float tangensAngleBottom = heightDifferenceBottom * mapGenerationConfiguration.HeightMultiplier / 1.0;
	
	float flowLeft = 0;
	float talusAngle = TangensTalusAngle(id);
	if (tangensAngleLeft > talusAngle)
	{
		flowLeft = heightDifferenceLeft;
	}

	float flowRight = 0;
	if (tangensAngleRight > talusAngle)
	{
		flowRight = heightDifferenceRight;
	}

	float FlowUp = 0;
	if (tangensAngleTop > talusAngle)
	{
		FlowUp = heightDifferenceTop;
	}

	float FlowDown = 0;
	if (tangensAngleBottom > talusAngle)
	{
		FlowDown = heightDifferenceBottom;
	}

	// Output flux
	float totalHeightDifference = heightDifferenceLeft + heightDifferenceRight + heightDifferenceBottom + heightDifferenceTop;

	if (totalHeightDifference > 0)
	{
		if(x > 0)
		{
			gridThermalErosionCell.FlowLeft = max(volumeToBeMoved * flowLeft / totalHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.FlowLeft = 0;
		}
		
		if(x < myHeightMapSideLength - 1)
		{
			gridThermalErosionCell.FlowRight = max(volumeToBeMoved * flowRight / totalHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.FlowRight = 0;
		}

		if(y > 0)
		{
			gridThermalErosionCell.FlowDown = max(volumeToBeMoved * FlowDown / totalHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.FlowDown = 0;
		}
		
		if(y < myHeightMapSideLength - 1)
		{
			gridThermalErosionCell.FlowUp = max(volumeToBeMoved * FlowUp / totalHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.FlowUp = 0;
		}
	}

	gridThermalErosionCells[id] = gridThermalErosionCell;
	
	memoryBarrier();
}