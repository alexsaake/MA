#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct ParticleHydraulicErosion
{
    int Age;
    float Volume;
    float Sediment;
    vec2 Position;
    vec2 Speed;
};

layout(std430, binding = 2) buffer particleHydraulicErosionShaderBuffer
{
    ParticleHydraulicErosion[] particlesHydraulicErosion;
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
	bool IsWaterKeptInBoundaries;
};

layout(std430, binding = 6) readonly restrict buffer erosionConfigurationShaderBuffer
{
    ErosionConfiguration erosionConfiguration;
};

struct ParticleHydraulicErosionConfiguration
{
    float WaterIncrease;
    uint MaxAge;
    float EvaporationRate;
    float DepositionRate;
    float MinimumVolume;
    float MaximalErosionDepth;
    float Gravity;
    bool AreParticlesAdded;
};

layout(std430, binding = 7) readonly restrict buffer particleHydraulicErosionConfigurationShaderBuffer
{
    ParticleHydraulicErosionConfiguration particleHydraulicErosionConfiguration;
};

layout(std430, binding = 11) readonly restrict buffer hydraulicErosionHeightMapIndicesShaderBuffer
{
    uint[] hydraulicErosionHeightMapIndices;
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

uint GetIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
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

//https://github.com/erosiv/soillib/blob/main/source/particle/water.hpp
ParticleHydraulicErosion myParticleHydraulicErosion;
vec2 myOriginalPosition;
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

vec3 getScaledNormal(uint x, uint y)
{
    if (x < 1 || x > myHeightMapSideLength - 2
        || y < 1 || y > myHeightMapSideLength - 2)
    {
        return vec3(0.0, 0.0, 1.0);
    }
    
    float rb = totalHeight(GetIndex(x + 1, y - 1));
    float lb = totalHeight(GetIndex(x - 1, y - 1));
    float r = totalHeight(GetIndex(x + 1, y));
    float l = totalHeight(GetIndex(x - 1, y));
    float rt = totalHeight(GetIndex(x + 1, y + 1));
    float lt = totalHeight(GetIndex(x - 1, y + 1));
    float t = totalHeight(GetIndex(x, y + 1));
    float b = totalHeight(GetIndex(x, y - 1));

    vec3 normal = vec3(
    mapGenerationConfiguration.HeightMultiplier * -(rb - lb + 2 * (r - l) + rt - lt),
    mapGenerationConfiguration.HeightMultiplier * -(lt - lb + 2 * (t - b) + rt - rb),
    1.0);

    return normalize(normal);
}

bool Move()
{
    const ivec2 position = ivec2(myParticleHydraulicErosion.Position);

    if(IsOutOfBounds(position))
    {
        myParticleHydraulicErosion.Sediment = 0;
        myParticleHydraulicErosion.Volume = 0;
        return false;
    }

    if(myParticleHydraulicErosion.Age > particleHydraulicErosionConfiguration.MaxAge
    || myParticleHydraulicErosion.Volume < particleHydraulicErosionConfiguration.MinimumVolume
    || totalHeight(GetIndexVector(position)) <= erosionConfiguration.SeaLevel - particleHydraulicErosionConfiguration.MaximalErosionDepth)
    {
        DepositeOnTop(GetIndexVector(position), myParticleHydraulicErosion.Sediment);
        myParticleHydraulicErosion.Sediment = 0;
        myParticleHydraulicErosion.Volume = 0;
        return false;
    }

    const vec3 normal = getScaledNormal(position.x, position.y);

    myParticleHydraulicErosion.Speed += particleHydraulicErosionConfiguration.Gravity * normal.xy / myParticleHydraulicErosion.Volume;

    if(length(myParticleHydraulicErosion.Speed) > 0)
    {
        myParticleHydraulicErosion.Speed = sqrt(2.0) * normalize(myParticleHydraulicErosion.Speed);
    }

    myOriginalPosition = myParticleHydraulicErosion.Position;
    myParticleHydraulicErosion.Position += myParticleHydraulicErosion.Speed;

    return true;
}

bool Interact()
{
    const ivec2 originalPosition = ivec2(myOriginalPosition);

    if(IsOutOfBounds(originalPosition))
    {
        return false;
    }

    float currentHeight;
    if(IsOutOfBounds(ivec2(myParticleHydraulicErosion.Position)))
    {
        currentHeight = 0.99 * totalHeight(GetIndexVector(originalPosition));
    }
    else
    {
        ivec2 currentPosition = ivec2(myParticleHydraulicErosion.Position);
        currentHeight = totalHeight(GetIndexVector(currentPosition));
    }

    float heightDifference = totalHeight(GetIndexVector(originalPosition)) - currentHeight;
    if(heightDifference < 0)
    {
        heightDifference = 0;
    }

    float capacity = (heightDifference * myParticleHydraulicErosion.Volume - myParticleHydraulicErosion.Sediment);

    if(capacity < 0)
    {
        if(particleHydraulicErosionConfiguration.DepositionRate * capacity < -myParticleHydraulicErosion.Sediment)
        {
            capacity = -myParticleHydraulicErosion.Sediment / particleHydraulicErosionConfiguration.DepositionRate;
        }
    }

    float difference = particleHydraulicErosionConfiguration.DepositionRate * capacity;
    if(difference > 0)
    {
        float suspendedSediment = SuspendFromTop(GetIndexVector(originalPosition), difference);
        myParticleHydraulicErosion.Sediment += suspendedSediment;
    }
    else
    {
        DepositeOnTop(GetIndexVector(originalPosition), abs(difference));
        myParticleHydraulicErosion.Sediment += difference;
    }

    myParticleHydraulicErosion.Volume *= (1.0 - particleHydraulicErosionConfiguration.EvaporationRate);

    myParticleHydraulicErosion.Age++;

    return true;
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    myHeightMapLength = heightMap.length() / mapGenerationConfiguration.LayerCount;
    if(id >= particlesHydraulicErosion.length())
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapLength));

    myParticleHydraulicErosion = particlesHydraulicErosion[id];

    if(myParticleHydraulicErosion.Volume == 0 && particleHydraulicErosionConfiguration.AreParticlesAdded)
    {
        uint index = hydraulicErosionHeightMapIndices[id];
        uint x = index % myHeightMapSideLength;
        uint y = index / myHeightMapSideLength;
        myParticleHydraulicErosion.Age = 0;
        myParticleHydraulicErosion.Volume = particleHydraulicErosionConfiguration.WaterIncrease;
        myParticleHydraulicErosion.Sediment = 0.0;
        myParticleHydraulicErosion.Position = vec2(x, y);
        myParticleHydraulicErosion.Speed = vec2(0.0, 0.0);
    }
    
    if(Move())
    {
        Interact();
    }
    
    particlesHydraulicErosion[id] = myParticleHydraulicErosion;

    memoryBarrier();
}