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

uint getIndexVector(vec2 position)
{
    return uint((position.y * myHeightMapSideLength) + position.x);
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute

void main()
{    
    float timeDelta = 0.25;
    float cellSizeX = 1.0 / 256;
    float cellSizeY = 1.0 / 256;
    float maximalErosionDepth = 10.0;
    float sedimentCapacity = 1.0;
    float suspensionRate = 0.5;
    float depositionRate = 1.0;
    float evaporationRate = 0.015;
    float sedimentSofteningRate = 0.0;

    uint id = gl_GlobalInvocationID.x;
    uint myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridPoint gridPoint = gridPoints[id];

	vec3 dhdx = vec3(2 * cellSizeX, heightMap[getIndex(x + 1, y)] - heightMap[getIndex(x - 1, y)], 0);
	vec3 dhdy = vec3(0, heightMap[getIndex(x, y + 1)] - heightMap[getIndex(x, y - 1)], 2 * cellSizeY);
	vec3 normal = cross(dhdx, dhdy);

	float sinTiltAngle = abs(normal.y) / length(normal);
	
	float lmax = clamp(1 - max(0, maximalErosionDepth - gridPoint.WaterHeight) / maximalErosionDepth, 0.0, 1.0);
	float sedimentTransportCapacity = sedimentCapacity * length(vec2(gridPoint.VelocityX, gridPoint.VelocityY)) * min(sinTiltAngle, 0.05) * lmax;

	if (gridPoint.SuspendedSediment < sedimentTransportCapacity)
	{
		float mod = timeDelta * suspensionRate * gridPoint.Hardness * (sedimentTransportCapacity - gridPoint.SuspendedSediment);		
		heightMap[id] -= mod;
		gridPoint.SuspendedSediment += mod;
	}
	else
	{
		float mod = timeDelta * depositionRate * (gridPoint.SuspendedSediment - sedimentTransportCapacity);
		heightMap[id] += mod;
		gridPoint.SuspendedSediment -= mod;
	}	

	gridPoint.WaterHeight *= 1 - evaporationRate * timeDelta;
	 
	// Hardness update
	gridPoint.Hardness -= timeDelta * sedimentSofteningRate * suspensionRate * (gridPoint.SuspendedSediment - sedimentTransportCapacity);
	gridPoint.Hardness = clamp(gridPoint.Hardness, 0.1, 1.0);
	
    gridPoints[id] = gridPoint;
}