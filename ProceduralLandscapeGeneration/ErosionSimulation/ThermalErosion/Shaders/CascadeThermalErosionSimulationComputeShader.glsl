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

float HeightTopmostLayer(uint index)
{
	for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
	{
        float height = heightMap[index + layer * myHeightMapLength];
		if(height > 0)
		{
			return height;
		}
	}
	return 0;   
}

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

float RemoveFromTop(uint index, float sediment)
{
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        uint offsetIndex = index + layer * myHeightMapLength;
        float height = heightMap[offsetIndex];
        if(height > 0)
        {
            if(height > sediment)
            {
                heightMap[offsetIndex] -= sediment;
                return sediment;
            }
            else
            {
                heightMap[offsetIndex] = 0;
                return sediment - height;
            }
        }
    }
    return 0;
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

    uint index = getIndexV(position);

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
        ivec2 Position;
        float TotalHeight;
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

        neighborCells[neighbors].Position = neighborPosition;
        neighborCells[neighbors].TotalHeight = totalHeight(getIndexV(neighborPosition));
        neighbors++;
    }

    // Local Matrix, Target Height

    float totalHeight = totalHeight(index);
    float tangensTalusAngle = TangensTalusAngle(index);
    for (int neighbor = 0; neighbor < neighbors; neighbor++)
    {
        // Full Height-Different Between Positions!
        float heightDifference = totalHeight - neighborCells[neighbor].TotalHeight;
	    float tangensAngle = heightDifference * mapGenerationConfiguration.HeightMultiplier / 1.0;
        if (heightDifference < 0
            || tangensAngle < tangensTalusAngle)
        {
            continue;
        }

        // Actual Amount Transferred
        float heightTopmostLayer = HeightTopmostLayer(index);
        float transfer = min(heightDifference, heightTopmostLayer) * thermalErosionConfiguration.ErosionRate * erosionConfiguration.TimeDelta;
        
        float removedSediment = RemoveFromTop(index, transfer);
        DepositeOnTop(getIndexV(neighborCells[neighbor].Position), removedSediment);
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