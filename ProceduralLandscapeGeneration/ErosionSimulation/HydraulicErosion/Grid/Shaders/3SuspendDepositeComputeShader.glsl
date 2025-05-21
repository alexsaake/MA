#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct GridHydraulicErosionCell
{
    float WaterHeight;

    float WaterFlowLeft;
    float WaterFlowRight;
    float WaterFlowUp;
    float WaterFlowDown;

    float SuspendedSediment;

    float SedimentFlowLeft;
    float SedimentFlowRight;
    float SedimentFlowUp;
    float SedimentFlowDown;

    vec2 WaterVelocity;
};

layout(std430, binding = 4) buffer gridHydraulicErosionCellShaderBuffer
{
    GridHydraulicErosionCell[] gridHydraulicErosionCells;
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

struct GridErosionConfiguration
{
    float WaterIncrease;
    float Gravity;
    float Dampening;
    float MaximalErosionDepth;
    float SedimentCapacity;
    float SuspensionRate;
    float DepositionRate;
    float EvaporationRate;
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

layout(std430, binding = 9) buffer gridErosionConfigurationShaderBuffer
{
    GridErosionConfiguration gridErosionConfiguration;
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

float SuspendFromTop(uint index, float requiredSediment)
{
    float suspendedSediment = 0;
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        uint offsetIndex = index + layer * myHeightMapLength;
        float height = heightMap[offsetIndex];
        float hardness = (1.0 - layersConfiguration[layer].Hardness);
        float toBeSuspendedSediment = requiredSediment * hardness;
        if(height >= toBeSuspendedSediment)
        {
            heightMap[offsetIndex] -= toBeSuspendedSediment;
            suspendedSediment += toBeSuspendedSediment;
            break;
        }
        else
        {
            heightMap[offsetIndex] = 0;
            requiredSediment -= height * hardness;
            suspendedSediment += height * hardness;
        }
    }
    return suspendedSediment;
}

void DepositeOnTop(uint index, float sediment)
{
    heightMap[index + (mapGenerationConfiguration.LayerCount - 1) * myHeightMapLength] += sediment;
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

uint GetIndex(uint x, uint y)
{
    return uint((y * myHeightMapSideLength) + x);
}

uint GetIndexVector(vec2 position)
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
    myHeightMapLength = heightMap.length() / mapGenerationConfiguration.LayerCount;
    if(id >= myHeightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapLength));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridHydraulicErosionCell gridHydraulicErosionCell = gridHydraulicErosionCells[id];

    float height = totalHeight(id);
    float heightLeft;
    float heightRight;
    float heightDown;
    float heightUp;
    if(x > 0)
    {
        heightLeft = totalHeight(GetIndex(x - 1, y));
    }
    else
    {
        heightLeft = height;
    }
    if(x < myHeightMapSideLength - 1)
    {
        heightRight = totalHeight(GetIndex(x + 1, y));
    }
    else
    {
        heightRight = height;
    }
    if(y > 0)
    {
        heightDown = totalHeight(GetIndex(x, y - 1));
    }
    else
    {
        heightDown = height;
    }
    if(y < myHeightMapSideLength - 1)
    {
        heightUp = totalHeight(GetIndex(x, y + 1));
    }
    else
    {
        heightUp = height;
    }

	vec3 dhdx = vec3(1.0, 0.0, (heightRight - heightLeft) / 2.0 * mapGenerationConfiguration.HeightMultiplier);
	vec3 dhdy = vec3(0.0, 1.0, (heightUp - heightDown) / 2.0 * mapGenerationConfiguration.HeightMultiplier);
	vec3 normal = normalize(cross(dhdx, dhdy));
    
    float dotProd = dot(normal, vec3(0.0, 0.0, 1.0));
    float alpha = acos(dotProd);
    float tiltAngle = sin(alpha);
	
	float erosionDepthLimit = (gridErosionConfiguration.MaximalErosionDepth - min(gridErosionConfiguration.MaximalErosionDepth, gridHydraulicErosionCell.WaterHeight)) / gridErosionConfiguration.MaximalErosionDepth;
	float sedimentCapacity = gridErosionConfiguration.SedimentCapacity * tiltAngle * length(gridHydraulicErosionCell.WaterVelocity) * erosionDepthLimit;

	if (sedimentCapacity > gridHydraulicErosionCell.SuspendedSediment)
	{
		float soilSuspended = max(gridErosionConfiguration.SuspensionRate * (sedimentCapacity - gridHydraulicErosionCell.SuspendedSediment) * erosionConfiguration.TimeDelta, 0.0);
		float suspendedSediment = SuspendFromTop(id, soilSuspended);
		gridHydraulicErosionCell.SuspendedSediment += suspendedSediment;
	}
	else if (sedimentCapacity < gridHydraulicErosionCell.SuspendedSediment)
	{
		float soilDeposited = min(gridErosionConfiguration.DepositionRate * (gridHydraulicErosionCell.SuspendedSediment - sedimentCapacity) * erosionConfiguration.TimeDelta, gridHydraulicErosionCell.SuspendedSediment);
		DepositeOnTop(id, soilDeposited);
		gridHydraulicErosionCell.SuspendedSediment -= soilDeposited;
	}
	
    gridHydraulicErosionCells[id] = gridHydraulicErosionCell;
    
    memoryBarrier();
}