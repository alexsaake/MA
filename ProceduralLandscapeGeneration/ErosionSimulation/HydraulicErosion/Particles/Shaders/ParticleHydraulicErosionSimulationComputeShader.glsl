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
    bool IsColorEnabled;
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

uint myHeightMapSideLength;

uint getIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

uint getIndexV(ivec2 position)
{
    return (position.y * myHeightMapSideLength) + position.x;
}

bool isOutOfBounds(ivec2 position)
{
    return position.x < 0 || position.x >= myHeightMapSideLength || position.y < 0 || position.y >= myHeightMapSideLength;
}

//https://github.com/erosiv/soillib/blob/main/source/particle/water.hpp
ParticleHydraulicErosion myParticleHydraulicErosion;
vec2 myOriginalPosition;

vec3 getScaledNormal(uint x, uint y)
{
    if (x < 1 || x > myHeightMapSideLength - 2
        || y < 1 || y > myHeightMapSideLength - 2)
    {
        return vec3(0.0, 0.0, 1.0);
    }

    float rb = heightMap[getIndex(x + 1, y - 1)];
    float lb = heightMap[getIndex(x - 1, y - 1)];
    float r = heightMap[getIndex(x + 1, y)];
    float l = heightMap[getIndex(x - 1, y)];
    float rt = heightMap[getIndex(x + 1, y + 1)];
    float lt = heightMap[getIndex(x - 1, y + 1)];
    float t = heightMap[getIndex(x, y + 1)];
    float b = heightMap[getIndex(x, y - 1)];

    vec3 normal = vec3(
    mapGenerationConfiguration.HeightMultiplier * -(rb - lb + 2 * (r - l) + rt - lt),
    mapGenerationConfiguration.HeightMultiplier * -(lt - lb + 2 * (t - b) + rt - rb),
    1.0);

    return normalize(normal);
}

bool Move()
{
    const ivec2 position = ivec2(myParticleHydraulicErosion.Position);

    if(isOutOfBounds(position))
    {
        myParticleHydraulicErosion.Sediment = 0;
        myParticleHydraulicErosion.Volume = 0;
        return false;
    }

    if(myParticleHydraulicErosion.Age > particleHydraulicErosionConfiguration.MaxAge
    || myParticleHydraulicErosion.Volume < particleHydraulicErosionConfiguration.MinimumVolume
    || heightMap[getIndexV(position)] <= erosionConfiguration.SeaLevel - particleHydraulicErosionConfiguration.MaximalErosionDepth)
    {
        heightMap[getIndexV(position)] += myParticleHydraulicErosion.Sediment;
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
    const ivec2 position = ivec2(myOriginalPosition);

    if(isOutOfBounds(position))
    {
        return false;
    }

    float h2;
    if(isOutOfBounds(ivec2(myParticleHydraulicErosion.Position)))
    {
        h2 = 0.99 * heightMap[getIndexV(position)];
    }
    else
    {
        ivec2 currentPosition = ivec2(myParticleHydraulicErosion.Position);
        h2 = heightMap[getIndexV(currentPosition)];
    }

    float cEq = heightMap[getIndexV(position)] - h2;
    if(cEq < 0)
    {
        cEq = 0;
    }

    float cDiff = (cEq * myParticleHydraulicErosion.Volume - myParticleHydraulicErosion.Sediment);

    float effD = particleHydraulicErosionConfiguration.DepositionRate;
    if(effD < 0)
    {
        effD = 0;
    }

    if(effD * cDiff < 0)
    {
        if(effD * cDiff < -myParticleHydraulicErosion.Sediment)
        {
            cDiff = -myParticleHydraulicErosion.Sediment / effD;
        }
    }

    myParticleHydraulicErosion.Sediment += effD * cDiff;
    heightMap[getIndexV(position)] -= effD * cDiff;

    myParticleHydraulicErosion.Volume *= (1.0 - particleHydraulicErosionConfiguration.EvaporationRate);

    myParticleHydraulicErosion.Age++;

    return true;
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    if(id >= particlesHydraulicErosion.length())
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(heightMap.length()));

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