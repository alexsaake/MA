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
    uint RockTypeCount;
    uint LayerCount;
    float SeaLevel;
    bool AreTerrainColorsEnabled;
    bool ArePlateTectonicsPlateColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

struct ErosionConfiguration
{
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
uint myHeightMapPlaneSize;

float SuspendFromTop(uint index, float requiredSediment)
{
    float suspendedSediment = 0;
    for(int rockType = int(mapGenerationConfiguration.RockTypeCount) - 1; rockType >= 0; rockType--)
    {
        uint offsetIndex = index + rockType * myHeightMapPlaneSize;
        float height = heightMap[offsetIndex];
        float hardness = (1.0 - rockTypesConfiguration[rockType].Hardness);
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
    heightMap[index + (mapGenerationConfiguration.RockTypeCount - 1) * myHeightMapPlaneSize] += sediment;
}

uint LayerOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

float TotalHeight(uint index, uint layer)
{
    float height = 0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapPlaneSize + LayerOffset(layer)];
    }
    return height;
}

vec3 GetScaledNormal(uint x, uint y, uint layer)
{
    if (x < 1 || x > myHeightMapSideLength - 2
        || y < 1 || y > myHeightMapSideLength - 2)
    {
        return vec3(0.0, 0.0, 1.0);
    }
    
    float rb = TotalHeight(GetIndex(x + 1, y - 1), layer);
    float lb = TotalHeight(GetIndex(x - 1, y - 1), layer);
    float r = TotalHeight(GetIndex(x + 1, y), layer);
    float l = TotalHeight(GetIndex(x - 1, y), layer);
    float rt = TotalHeight(GetIndex(x + 1, y + 1), layer);
    float lt = TotalHeight(GetIndex(x - 1, y + 1), layer);
    float t = TotalHeight(GetIndex(x, y + 1), layer);
    float b = TotalHeight(GetIndex(x, y - 1), layer);

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
    || TotalHeight(GetIndexVector(position), 0) <= mapGenerationConfiguration.SeaLevel - particleHydraulicErosionConfiguration.MaximalErosionDepth)
    {
        DepositeOnTop(GetIndexVector(position), myParticleHydraulicErosion.Sediment);
        myParticleHydraulicErosion.Sediment = 0;
        myParticleHydraulicErosion.Volume = 0;
        return false;
    }

    const vec3 normal = GetScaledNormal(position.x, position.y, 0);

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
        currentHeight = 0.99 * TotalHeight(GetIndexVector(originalPosition), 0);
    }
    else
    {
        ivec2 currentPosition = ivec2(myParticleHydraulicErosion.Position);
        currentHeight = TotalHeight(GetIndexVector(currentPosition), 0);
    }

    float heightDifference = TotalHeight(GetIndexVector(originalPosition), 0) - currentHeight;
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
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(id >= particlesHydraulicErosion.length())
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapPlaneSize));

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