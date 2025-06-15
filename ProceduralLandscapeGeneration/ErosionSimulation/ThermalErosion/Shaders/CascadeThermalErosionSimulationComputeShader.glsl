#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    uint RockTypeCount;
    uint LayerCount;
    float SeaLevel;
    bool AreTerrainColorsEnabled;
    bool ArePlateTectonicsPlateColorsEnabled;
    bool AreLayerColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

struct ErosionConfiguration
{
    float DeltaTime;
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

struct RockTypeConfiguration
{
    float Hardness;
    float TangensAngleOfRepose;
    float CollapseThreshold;
};

layout(std430, binding = 18) buffer rockTypesConfigurationShaderBuffer
{
    RockTypeConfiguration[] rockTypesConfiguration;
};

uint myHeightMapSideLength;
uint myHeightMapPlaneSize;

float TangensAngleOfRepose(uint index)
{
	for(int rockType = int(mapGenerationConfiguration.RockTypeCount) - 1; rockType >= 0; rockType--)
	{
		if(heightMap[index + rockType * myHeightMapPlaneSize] > 0)
		{
			return rockTypesConfiguration[rockType].TangensAngleOfRepose;
		}
	}
	return rockTypesConfiguration[0].TangensAngleOfRepose;
}

void RemoveFromTop(uint index, float sediment)
{
    for(int rockType = int(mapGenerationConfiguration.RockTypeCount) - 1; rockType >= 0; rockType--)
    {
        uint offsetIndex = index + rockType * myHeightMapPlaneSize;
        float height = heightMap[offsetIndex];
        if(height > 0)
        {
            if(height >= sediment)
            {
                heightMap[offsetIndex] -= sediment;
            }
            else
            {
                heightMap[offsetIndex] = 0;
            }
        }
    }
}

void DepositeOnTop(uint index, float sediment)
{
    heightMap[index + (mapGenerationConfiguration.RockTypeCount - 1) * myHeightMapPlaneSize] += sediment;
}

float TotalHeight(uint index)
{
    float height = 0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapPlaneSize];
    }
    return height;
}

uint GetIndexVector(ivec2 position)
{
    return (position.y * myHeightMapSideLength) + position.x;
}

bool IsOutOfBounds(ivec2 position)
{
    return position.x < 0 || position.x >= myHeightMapSideLength
        || position.y < 0 || position.y >= myHeightMapSideLength;
}

//https://github.com/erosiv/soillib/blob/main/source/particle/cascade.hpp

void main()
{
    uint index = gl_GlobalInvocationID.x;
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= myHeightMapPlaneSize)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapPlaneSize));

    uint x = index % myHeightMapSideLength;
    uint y = index / myHeightMapSideLength;

    ivec2 position = ivec2(x, y);
    
    if(IsOutOfBounds(position))
    {
        return;
    }

    // Get Non-Out-of-Bounds Neighbors

    const ivec2 neighboringPositions[] = {
        ivec2(-1, -1),
        ivec2(-1, 0),
        ivec2(-1, 1),
        ivec2(0, -1),
        ivec2(0, 1),
        ivec2(1, -1),
        ivec2(1, 0),
        ivec2(1, 1)
    };

    struct Point
    {
        uint Index;
        float TotalHeight;
        float Distance;
    } neighborCells[8];

    int neighbors = 0;
    for(int neighbor = 0; neighbor < neighboringPositions.length(); neighbor++)
    {
        ivec2 neighboringPosition = neighboringPositions[neighbor];
        ivec2 neighborPosition = position + neighboringPosition;

        if(IsOutOfBounds(neighborPosition))
        {
            continue;
        }
        
            uint neightborIndex = GetIndexVector(neighborPosition);
        neighborCells[neighbors].Index = neightborIndex;
        neighborCells[neighbors].TotalHeight = TotalHeight(neightborIndex);
        neighborCells[neighbors].Distance = length(neighboringPosition);
        neighbors++;
    }

    // Local Matrix, Target Height
    float heightAverage = TotalHeight(index);
    for(int neighbor = 0; neighbor < neighbors; neighbor++)
    {
        heightAverage += neighborCells[neighbor].TotalHeight;
    }
    heightAverage /= float(neighbors + 1);

    float tangensAngleOfRepose = TangensAngleOfRepose(index);
    for (int neighbor = 0; neighbor < neighbors; neighbor++)
    {
        // Full Height-Different Between Positions!
        float heightDifference = heightAverage - neighborCells[neighbor].TotalHeight;
	    float tangensAngle = heightDifference * mapGenerationConfiguration.HeightMultiplier / 1.0 / neighborCells[neighbor].Distance;
        if (heightDifference < 0
            || tangensAngle < tangensAngleOfRepose)
        {
            continue;
        }

        // Actual Amount Transferred
        float transfer = heightDifference * thermalErosionConfiguration.ErosionRate * erosionConfiguration.DeltaTime;
        
        RemoveFromTop(index, transfer);
        DepositeOnTop(neighborCells[neighbor].Index, transfer);
    }
    
    memoryBarrier();
}