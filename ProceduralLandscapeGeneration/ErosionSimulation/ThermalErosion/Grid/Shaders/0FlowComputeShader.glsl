#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    bool IsColorEnabled;
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
    float TangensTalusAngle;
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
    float FlowTop;
    float FlowBottom;
};

layout(std430, binding = 13) buffer gridThermalErosionCellShaderBuffer
{
    GridThermalErosionCell[] gridThermalErosionCells;
};

uint myHeightMapSideLength;

uint getIndex(uint x, uint y)
{
    return uint((y * myHeightMapSideLength) + x);
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute

void main()
{	
    uint id = gl_GlobalInvocationID.x;
    if(id >= heightMap.length())
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(gridThermalErosionCells.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[id];

	float heightDifferenceLeft = max(heightMap[id] - heightMap[getIndex(x - 1, y)], 0.0);
	float heightDifferenceRight = max(heightMap[id] - heightMap[getIndex(x + 1, y)], 0.0);
	float heightDifferenceTop = max(heightMap[id] - heightMap[getIndex(x, y + 1)], 0.0);
	float heightDifferenceBottom = max(heightMap[id] - heightMap[getIndex(x, y - 1)], 0.0);
	float maxHeightDifference = max(max(heightDifferenceLeft, heightDifferenceRight), max(heightDifferenceBottom, heightDifferenceTop));

	float volumeToBeMoved = maxHeightDifference * thermalErosionConfiguration.ErosionRate * (1.0 - thermalErosionConfiguration.Dampening);
	
	float tangensAngleLeft = heightDifferenceLeft * mapGenerationConfiguration.HeightMultiplier / 1.0;
	float tangensAngleRight = heightDifferenceRight * mapGenerationConfiguration.HeightMultiplier / 1.0;
	float tangensAngleTop = heightDifferenceTop * mapGenerationConfiguration.HeightMultiplier / 1.0;
	float tangensAngleBottom = heightDifferenceBottom * mapGenerationConfiguration.HeightMultiplier / 1.0;
	
	float flowLeft = 0;
	if (tangensAngleLeft > thermalErosionConfiguration.TangensTalusAngle)
	{
		flowLeft = heightDifferenceLeft;
	}

	float flowRight = 0;
	if (tangensAngleRight > thermalErosionConfiguration.TangensTalusAngle)
	{
		flowRight = heightDifferenceRight;
	}

	float flowTop = 0;
	if (tangensAngleTop > thermalErosionConfiguration.TangensTalusAngle)
	{
		flowTop = heightDifferenceTop;
	}

	float flowBottom = 0;
	if (tangensAngleBottom > thermalErosionConfiguration.TangensTalusAngle)
	{
		flowBottom = heightDifferenceBottom;
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
			gridThermalErosionCell.FlowBottom = max(volumeToBeMoved * flowBottom / totalHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.FlowBottom = 0;
		}
		
		if(y < myHeightMapSideLength - 1)
		{
			gridThermalErosionCell.FlowTop = max(volumeToBeMoved * flowTop / totalHeightDifference * erosionConfiguration.TimeDelta, 0.0);
		}
		else
		{
			gridThermalErosionCell.FlowTop = 0;
		}
	}

	gridThermalErosionCells[id] = gridThermalErosionCell;
	
	memoryBarrier();
}