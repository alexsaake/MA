#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
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
    float BedrockHardness;
    float BedrockTangensTalusAngle;
};

layout(std430, binding = 19) buffer layersConfigurationShaderBuffer
{
    LayersConfiguration layersConfiguration;
};

uint myHeightMapSideLength;

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

        float height = heightMap[getIndexV(neighborPosition)];
        neighborCells[neighbors].position = neighborPosition;
        neighborCells[neighbors].height = height;
        neighborCells[neighbors].distance = length(neighboringPosition);
        neighbors++;
    }

    // Local Matrix, Target Height

    float heightAverage = heightMap[getIndexV(position)];
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
        float excess = abs(heightDifference) - neighborCells[neighbor].distance * layersConfiguration.BedrockTangensTalusAngle / mapGenerationConfiguration.HeightMultiplier;
        if (excess <= 0)
        {
            continue;
        }

        // Actual Amount Transferred
        float transfer = excess * thermalErosionConfiguration.ErosionRate * erosionConfiguration.TimeDelta * (1.0 - thermalErosionConfiguration.Dampening);
        heightMap[tindex] -= transfer;
        heightMap[bindex] += transfer;
    }
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    if(id >= heightMap.length())
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;

    Cascade(ivec2(x, y));
    
    memoryBarrier();
}