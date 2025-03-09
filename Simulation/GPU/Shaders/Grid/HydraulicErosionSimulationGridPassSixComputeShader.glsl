#version 430

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 1) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct GridPoint
{
    float WaterHeight;
    float SuspendedSediment;
    float TempSediment;
    float Hardness;

    float FlowLeft;
    float FlowRight;
    float FlowTop;
    float FlowBottom;

    float ThermalLeft;
    float ThermalRight;
    float ThermalTop;
    float ThermalBottom;

    float VelocityX;
    float VelocityY;
};

layout(std430, binding = 2) buffer gridPointsShaderBuffer
{
    GridPoint[] gridPoints;
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
	float cellSizeX = 1.0 / 256;
	float cellSizeY = 1.0 / 256;
	float thermalErosionRate = 0.15;
	float talusAngleTangentCoeff = 0.8;
	float talusAngleTangentBias = 0.1;

	uint id = gl_GlobalInvocationID.x;
    uint myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridPoint gridPoint = gridPoints[id];

	float heightDifferenceL = heightMap[id] - heightMap[getIndex(x - 1, y)];
	float heightDifferenceR = heightMap[id] - heightMap[getIndex(x + 1, y)];
	float heightDifferenceT = heightMap[id] - heightMap[getIndex(x, y + 1)];
	float heightDifferenceB = heightMap[id] - heightMap[getIndex(x, y - 1)];
	float maxHeightDifference = max(max(heightDifferenceL, heightDifferenceR), max(heightDifferenceB, heightDifferenceT));

	float volumeToBeMoved = cellSizeX * cellSizeY * maxHeightDifference * 0.5 * thermalErosionRate * gridPoint.Hardness;
	
	float tanAngleL = heightDifferenceL / cellSizeX;
	float tanAngleR = heightDifferenceR / cellSizeX;
	float tanAngleT = heightDifferenceT / cellSizeY;
	float tanAngleB = heightDifferenceB / cellSizeY;
	
	float treshold = gridPoint.Hardness * talusAngleTangentCoeff + talusAngleTangentBias;
	
	float thermalLeft = 0;
	if (tanAngleL > treshold)
	{
		thermalLeft = heightDifferenceL;
	}

	float thermalRight = 0;
	if (tanAngleR > treshold)
	{
		thermalRight = heightDifferenceR;
	}

	float thermalTop = 0;
	if (tanAngleT > treshold)
	{
		thermalTop = heightDifferenceT;
	}

	float thermalBottom = 0;
	if (tanAngleB > treshold)
	{
		thermalBottom = heightDifferenceB;
	}

	// Output flux
	float sumProportions = heightDifferenceL + heightDifferenceR + heightDifferenceB + heightDifferenceT;

	if (sumProportions > 0)
	{
		if(x > 0)
		{
			gridPoint.ThermalLeft = volumeToBeMoved * thermalLeft / sumProportions;
		}
		else
		{
			gridPoint.ThermalLeft = 0;
		}
		
		if(x < myHeightMapSideLength - 1)
		{
			gridPoint.ThermalRight = volumeToBeMoved * thermalRight / sumProportions;
		}
		else
		{
			gridPoint.ThermalRight = 0;
		}

		if(y > 0)
		{
			gridPoint.ThermalBottom = volumeToBeMoved * thermalBottom / sumProportions;
		}
		else
		{
			gridPoint.ThermalBottom = 0;
		}
		
		if(y < myHeightMapSideLength - 1)
		{
			gridPoint.ThermalTop = volumeToBeMoved * thermalTop / sumProportions;
		}
		else
		{
			gridPoint.ThermalTop = 0;
		}

		gridPoints[id] = gridPoint;
	}
}