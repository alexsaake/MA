#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct GridPoint
{
    float WaterHeight;
    float SuspendedSediment;
    float TempSediment;
    float FlowLeft;
    float FlowRight;
    float FlowTop;
    float FlowBottom;    
    vec2 Velocity;
};

layout(std430, binding = 4) buffer gridPointsShaderBuffer
{
    GridPoint[] gridPoints;
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

struct GridErosionConfiguration
{
    float WaterIncrease;
    float TimeDelta;
    float Gravity;
    float Dampening;
    float MaximalErosionDepth;
    float SuspensionRate;
    float DepositionRate;
    float EvaporationRate;
};

layout(std430, binding = 9) buffer gridErosionConfigurationShaderBuffer
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
//https://github.com/keepitwiel/hydraulic-erosion-simulator/blob/main/src/algorithm.py
//https://github.com/karhu/terrain-erosion/blob/master/Simulation/FluidSimulation.cpp
//depth limit
//https://github.com/patiltanma/15618-FinalProject/blob/master/Renderer/Renderer/erosion_kernel.cu
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

    float height = heightMap[id];
    float heightLeft;
    float heightRight;
    float heightBottom;
    float heightTop;
    if(x > 0)
    {
        heightLeft = heightMap[getIndex(x - 1, y)];
    }
    else
    {
        heightLeft = height;
    }
    if(x < myHeightMapSideLength - 1)
    {
        heightRight = heightMap[getIndex(x + 1, y)];
    }
    else
    {
        heightRight = height;
    }
    if(y > 0)
    {
        heightBottom = heightMap[getIndex(x, y - 1)];
    }
    else
    {
        heightBottom = height;
    }
    if(y < myHeightMapSideLength - 1)
    {
        heightTop = heightMap[getIndex(x, y + 1)];
    }
    else
    {
        heightTop = height;
    }

	vec3 dhdx = vec3(1.0, 0.0, (heightRight - heightLeft) / 2.0 * mapGenerationConfiguration.HeightMultiplier);
	vec3 dhdy = vec3(0.0, 1.0, (heightTop - heightBottom) / 2.0 * mapGenerationConfiguration.HeightMultiplier);
	vec3 normal = normalize(cross(dhdx, dhdy));
    
    float dotProd = dot(normal, vec3(0.0, 0.0, 1.0));
    float alpha = acos(dotProd);
    float tiltAngle = sin(alpha);
	
    float sedimentCapacity = gridPoint.WaterHeight - gridPoint.SuspendedSediment;
	float erosionDepthLimit = (gridErosionConfiguration.MaximalErosionDepth - min(gridErosionConfiguration.MaximalErosionDepth, gridPoint.WaterHeight)) / gridErosionConfiguration.MaximalErosionDepth;
	float sedimentTransportCapacity = sedimentCapacity * tiltAngle * length(gridPoint.Velocity) * erosionDepthLimit;

	if (sedimentTransportCapacity > gridPoint.SuspendedSediment)
	{
		float soilSuspended = gridErosionConfiguration.TimeDelta * gridErosionConfiguration.SuspensionRate * (sedimentTransportCapacity - gridPoint.SuspendedSediment);
		heightMap[id] -= soilSuspended;
		gridPoint.SuspendedSediment += soilSuspended;
	}
	else if (sedimentTransportCapacity < gridPoint.SuspendedSediment)
	{
		float soilDeposited = gridErosionConfiguration.TimeDelta * gridErosionConfiguration.DepositionRate * (gridPoint.SuspendedSediment - sedimentTransportCapacity);
		heightMap[id] += soilDeposited;
		gridPoint.SuspendedSediment -= soilDeposited;
	}
	
    gridPoints[id] = gridPoint;
    
    memoryBarrier();
}