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

void RemoveFromTop(uint index, float sediment)
{
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        uint offsetIndex = index + layer * myHeightMapLength;
        float height = heightMap[offsetIndex];
        if(height >= sediment)
        {
            heightMap[offsetIndex] -= sediment;
            break;
        }
        else
        {
            heightMap[offsetIndex] = 0;
            sediment -= height;
        }
    }
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

uint getIndexV(ivec2 position)
{
    return (position.y * myHeightMapSideLength) + position.x;
}

bool IsOutOfBounds(ivec2 position)
{
    return position.x < 0 || position.x >= myHeightMapSideLength
        || position.y < 0 || position.y >= myHeightMapSideLength;
}

//https://github.com/erosiv/soillib/blob/main/source/particle/cascade.hpp
void Cascade(ivec2 position)
{
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

    struct Point {
        ivec2 position;
        float height;
        float distance;
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

        float height = totalHeight(getIndexV(neighborPosition));
        neighborCells[neighbors].position = neighborPosition;
        neighborCells[neighbors].height = height;
        neighborCells[neighbors].distance = length(neighboringPosition);
        neighbors++;
    }

    // Local Matrix, Target Height

    float heightAverage = totalHeight(getIndexV(position));
    for(int neighbor = 0; neighbor < neighbors; neighbor++)
    {
        heightAverage += neighborCells[neighbor].height;
    }
    heightAverage /= float(neighbors + 1);

    for (int neighbor = 0; neighbor < neighbors; neighbor++)
    {
        // Full Height-Different Between Positions!
        float heightDifference = heightAverage - neighborCells[neighbor].height;
        if (heightDifference == 0)
        {
            continue;
        }

        ivec2 tpos = (heightDifference > 0) ? position : neighborCells[neighbor].position;
        ivec2 bpos = (heightDifference > 0) ? neighborCells[neighbor].position : position;

        uint tindex = getIndexV(tpos);
        uint bindex = getIndexV(bpos);

        // The Amount of Excess Difference!
        float excess = abs(heightDifference) - neighborCells[neighbor].distance * TangensTalusAngle(gl_GlobalInvocationID.x) / mapGenerationConfiguration.HeightMultiplier;
        if (excess <= 0)
        {
            continue;
        }

        // Actual Amount Transferred
        float transfer = excess * thermalErosionConfiguration.ErosionRate * erosionConfiguration.TimeDelta * (1.0 - thermalErosionConfiguration.Dampening);
        
        if(transfer > 0)
        {
            RemoveFromTop(tindex, transfer);
            DepositeOnTop(bindex, transfer);
        }
        else
        {
            RemoveFromTop(bindex, abs(transfer));
            DepositeOnTop(tindex, abs(transfer));
        }
    }
}

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

    Cascade(ivec2(x, y));
    
    memoryBarrier();
}