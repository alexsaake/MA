#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
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
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute

void main()
{
	float dampening = 0.25;
	float thermalErosionRate = 0.15;
	float talusAngleTangentCoeff = 0.15;
	
    uint id = gl_GlobalInvocationID.x;
    uint heightMapLength = heightMap.length();
    if(id > heightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(gridThermalErosionCells.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[id];

	float heightDifferenceL = heightMap[id] - heightMap[getIndex(x - 1, y)];
	float heightDifferenceR = heightMap[id] - heightMap[getIndex(x + 1, y)];
	float heightDifferenceT = heightMap[id] - heightMap[getIndex(x, y + 1)];
	float heightDifferenceB = heightMap[id] - heightMap[getIndex(x, y - 1)];
	float maxHeightDifference = max(max(heightDifferenceL, heightDifferenceR), max(heightDifferenceB, heightDifferenceT));

	float volumeToBeMoved = maxHeightDifference * 0.5 * thermalErosionRate;
	
	float tanAngleL = heightDifferenceL;
	float tanAngleR = heightDifferenceR;
	float tanAngleT = heightDifferenceT;
	float tanAngleB = heightDifferenceB;
	
	float treshold = talusAngleTangentCoeff;
	
	float flowLeft = 0;
	if (tanAngleL > treshold)
	{
		flowLeft = heightDifferenceL;
	}

	float flowRight = 0;
	if (tanAngleR > treshold)
	{
		flowRight = heightDifferenceR;
	}

	float flowTop = 0;
	if (tanAngleT > treshold)
	{
		flowTop = heightDifferenceT;
	}

	float flowBottom = 0;
	if (tanAngleB > treshold)
	{
		flowBottom = heightDifferenceB;
	}

	// Output flux
	float sumProportions = heightDifferenceL + heightDifferenceR + heightDifferenceB + heightDifferenceT;

	if (sumProportions > 0)
	{
		if(x > 0)
		{
			gridThermalErosionCell.FlowLeft = volumeToBeMoved * flowLeft / sumProportions;
		}
		else
		{
			gridThermalErosionCell.FlowLeft = 0;
		}
		
		if(x < myHeightMapSideLength - 1)
		{
			gridThermalErosionCell.FlowRight = volumeToBeMoved * flowRight / sumProportions;
		}
		else
		{
			gridThermalErosionCell.FlowRight = 0;
		}

		if(y > 0)
		{
			gridThermalErosionCell.FlowBottom = volumeToBeMoved * flowBottom / sumProportions;
		}
		else
		{
			gridThermalErosionCell.FlowBottom = 0;
		}
		
		if(y < myHeightMapSideLength - 1)
		{
			gridThermalErosionCell.FlowTop = volumeToBeMoved * flowTop / sumProportions;
		}
		else
		{
			gridThermalErosionCell.FlowTop = 0;
		}
		
		gridThermalErosionCells[id] = gridThermalErosionCell;
		
		memoryBarrier();
	}
}