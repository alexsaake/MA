#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 1) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

layout(std430, binding = 2) readonly restrict buffer heightMapIndicesShaderBuffer
{
    uint[] heightMapIndices;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    float SeaLevel;
    bool IsColorEnabled;
};

layout(std430, binding = 3) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
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
    float MaxDiff;
    float Settling;
    bool IsWaterAdded;
};

layout(std430, binding = 4) readonly restrict buffer particleHydraulicErosionConfigurationShaderBuffer
{
    ParticleHydraulicErosionConfiguration particleHydraulicErosionConfiguration;
};

struct ParticleHydraulicErosion
{
    int Age;
    float Volume;
    float Sediment;
    vec2 Position;
    vec2 Speed;
};

layout(std430, binding = 5) buffer particleHydraulicErosionShaderBuffer
{
    ParticleHydraulicErosion[] particlesHydraulicErosion;
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
    return position.x < 0 || position.x > myHeightMapSideLength || position.y < 0 || position.y > myHeightMapSideLength;
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

//https://github.com/erosiv/soillib/blob/main/source/particle/cascade.hpp
void Cascade(ivec2 ipos)
{
    if(isOutOfBounds(ipos))
    {
        return;
    }

    // Get Non-Out-of-Bounds Neighbors

    const ivec2 n[] = {
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
        ivec2 pos;
        float h;
        float d;
    } sn[8];

    int num = 0;

    for(int i = 0; i < n.length(); i++)
    {
        ivec2 nn = n[i];
        ivec2 npos = ipos + nn;

        if(isOutOfBounds(npos))
        {
            continue;
        }

        float height = heightMap[getIndexV(npos)];
        sn[num].pos = npos;
        sn[num].h = height;
        sn[num].d = length(nn);
        num++;
    }

    // Local Matrix, Target Height

    float height = heightMap[getIndexV(ipos)];
    float h_ave = height;
    for(int i = 0; i < num; ++i)
    {
        h_ave += sn[i].h;
    }
    h_ave /= float(num + 1);

    for (int i = 0; i < num; ++i)
    {
        // Full Height-Different Between Positions!
        float diff = h_ave - sn[i].h;
        if (diff == 0)
        {
            continue;
        }

        ivec2 tpos = (diff > 0) ? ipos : sn[i].pos;
        ivec2 bpos = (diff > 0) ? sn[i].pos : ipos;

        uint tindex = getIndexV(tpos);
        uint bindex = getIndexV(bpos);

        // The Amount of Excess Difference!
        float excess = 0.0f;
        excess = abs(diff) - sn[i].d * particleHydraulicErosionConfiguration.MaxDiff;
        if (excess <= 0)
        {
            continue;
        }

        // Actual Amount Transferred
        float transfer = particleHydraulicErosionConfiguration.Settling * excess / 2.0f;
        heightMap[tindex] -= transfer;
        heightMap[bindex] += transfer;
    }
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
    || heightMap[getIndexV(position)] <= mapGenerationConfiguration.SeaLevel - particleHydraulicErosionConfiguration.MaximalErosionDepth)
    {
        heightMap[getIndexV(position)] += myParticleHydraulicErosion.Sediment;
        myParticleHydraulicErosion.Sediment = 0;
        myParticleHydraulicErosion.Volume = 0;
        Cascade(position);
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
        h2 = 0.99f * heightMap[getIndexV(position)];
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

    Cascade(position);

    myParticleHydraulicErosion.Age++;

    return true;
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    myHeightMapSideLength = uint(sqrt(heightMap.length()));
    if(id >= particlesHydraulicErosion.length())
    {
        return;
    }

    myParticleHydraulicErosion = particlesHydraulicErosion[id];

    if(myParticleHydraulicErosion.Volume == 0 && particleHydraulicErosionConfiguration.IsWaterAdded)
    {
        uint index = heightMapIndices[id];
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
}