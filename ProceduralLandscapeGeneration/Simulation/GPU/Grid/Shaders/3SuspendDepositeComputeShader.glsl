#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

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

struct GridErosionConfiguration
{
    float TimeDelta;
    float CellSizeX;
    float CellSizeY;
    float Gravity;
    float Friction;
    float MaximalErosionDepth;
    float SedimentCapacity;
    float SuspensionRate;
    float DepositionRate;
    float SedimentSofteningRate;
    float EvaporationRate;
};

layout(std430, binding = 3) buffer gridErosionConfigurationShaderBuffer
{
    GridErosionConfiguration gridErosionConfiguration;
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
	
    uint id = gl_GlobalInvocationID.x;
    uint heightMapLength = heightMap.length();
    if(id > heightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(gridPoints.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridPoint gridPoint = gridPoints[id];

	vec3 dhdx = vec3(2.0 * gridErosionConfiguration.CellSizeX, heightMap[getIndex(x + 1, y)] - heightMap[getIndex(x - 1, y)], 0.0);
	vec3 dhdy = vec3(0.0, heightMap[getIndex(x, y + 1)] - heightMap[getIndex(x, y - 1)], 2.0 * gridErosionConfiguration.CellSizeY);
	vec3 normal = cross(dhdx, dhdy);

	float sinTiltAngle = abs(normal.y) / length(normal);
	
	float lmax = clamp(1.0 - max(0.0, gridErosionConfiguration.MaximalErosionDepth - gridPoint.WaterHeight) / gridErosionConfiguration.MaximalErosionDepth, 0.0, 1.0);
	float sedimentTransportCapacity = gridErosionConfiguration.SedimentCapacity * length(vec2(gridPoint.VelocityX, gridPoint.VelocityY)) * sinTiltAngle * lmax;

	if (gridPoint.SuspendedSediment < sedimentTransportCapacity)
	{
		float mod = gridErosionConfiguration.TimeDelta * gridErosionConfiguration.SuspensionRate * gridPoint.Hardness * (sedimentTransportCapacity - gridPoint.SuspendedSediment);		
		heightMap[id] -= mod;
		gridPoint.SuspendedSediment += mod;
	}
	else if (gridPoint.SuspendedSediment > sedimentTransportCapacity)
	{
		float mod = gridErosionConfiguration.TimeDelta * gridErosionConfiguration.DepositionRate * (gridPoint.SuspendedSediment - sedimentTransportCapacity);
		heightMap[id] += mod;
		gridPoint.SuspendedSediment -= mod;
	}
	 
	// Hardness update
	gridPoint.Hardness -= gridErosionConfiguration.TimeDelta * gridErosionConfiguration.SedimentSofteningRate * gridErosionConfiguration.SuspensionRate * (gridPoint.SuspendedSediment - sedimentTransportCapacity);
	gridPoint.Hardness = clamp(gridPoint.Hardness, 0.1, 1.0);
	
    gridPoints[id] = gridPoint;
    
    memoryBarrier();
}