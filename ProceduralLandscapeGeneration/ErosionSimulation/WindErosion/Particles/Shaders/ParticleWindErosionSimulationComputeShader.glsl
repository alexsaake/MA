#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct ParticleWindErosion
{
    int Age;
    float Sediment;
    vec3 Position;
    vec3 Speed;
};

layout(std430, binding = 3) buffer particleWindErosionShaderBuffer
{
    ParticleWindErosion[] particlesWindErosion;
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

struct ParticleWindErosionConfiguration
{
    float SuspensionRate;
    float Gravity;
    uint MaxAge;
    vec2 PersistentSpeed;
    bool AreParticlesAdded;
};

layout(std430, binding = 8) readonly restrict buffer particleWindErosionConfigurationShaderBuffer
{
    ParticleWindErosionConfiguration particleWindErosionConfiguration;
};

layout(std430, binding = 14) readonly restrict buffer windErosionHeightMapIndicesShaderBuffer
{
    uint[] windErosionHeightMapIndices;
};

struct RockTypeConfiguration
{
    float Hardness;
    float TangensAngleOfRepose;
};

layout(std430, binding = 18) buffer rockTypesConfigurationShaderBuffer
{
    RockTypeConfiguration[] rockTypesConfiguration;
};

uint myHeightMapSideLength;
uint myHeightMapLength;

float SuspendFromTop(uint index, float requiredSediment)
{
    float suspendedSediment = 0;
    for(int rockType = int(mapGenerationConfiguration.RockTypeCount) - 1; rockType >= 0; rockType--)
    {
        uint offsetIndex = index + rockType * myHeightMapLength;
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
    heightMap[index + (mapGenerationConfiguration.RockTypeCount - 1) * myHeightMapLength] += sediment;
}

float TotalHeight(uint index, uint layer)
{
    float height = 0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapLength + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapLength];
    }
    return height;
}

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

//https://github.com/erosiv/soillib/blob/main/source/particle/wind.hpp
ParticleWindErosion myParticleWindErosion;
const float BoundaryLayer = 2.0;

vec3 GetScaledNormal(uint x, uint y)
{
    if (x < 1 || x > myHeightMapSideLength - 2
        || y < 1 || y > myHeightMapSideLength - 2)
    {
        return vec3(0.0, 0.0, 1.0);
    }
    
    float rb = TotalHeight(GetIndex(x + 1, y - 1));
    float lb = TotalHeight(GetIndex(x - 1, y - 1));
    float r = TotalHeight(GetIndex(x + 1, y));
    float l = TotalHeight(GetIndex(x - 1, y));
    float rt = TotalHeight(GetIndex(x + 1, y + 1));
    float lt = TotalHeight(GetIndex(x - 1, y + 1));
    float t = TotalHeight(GetIndex(x, y + 1));
    float b = TotalHeight(GetIndex(x, y - 1));

    vec3 normal = vec3(
    mapGenerationConfiguration.HeightMultiplier * -(rb - lb + 2 * (r - l) + rt - lt),
    mapGenerationConfiguration.HeightMultiplier * -(lt - lb + 2 * (t - b) + rt - rb),
    1.0);

    return normalize(normal);
}

//https://stackoverflow.com/questions/4200224/random-noise-functions-for-glsl
uint hash( uint x ) {
    x += ( x << 10u );
    x ^= ( x >>  6u );
    x += ( x <<  3u );
    x ^= ( x >> 11u );
    x += ( x << 15u );
    return x;
}

// Compound versions of the hashing algorithm I whipped together.
uint hash( uvec3 v ) { return hash( v.x ^ hash(v.y) ^ hash(v.z)             ); }

// Construct a float with half-open range [0:1] using low 23 bits.
// All zeroes yields 0.0, all ones yields the next smallest representable value below 1.0.
float floatConstruct( uint m ) {
    const uint ieeeMantissa = 0x007FFFFFu; // binary32 mantissa bitmask
    const uint ieeeOne      = 0x3F800000u; // 1.0 in IEEE binary32

    m &= ieeeMantissa;                     // Keep only mantissa bits (fractional part)
    m |= ieeeOne;                          // Add fractional part to 1.0

    float  f = uintBitsToFloat( m );       // Range [1:2]
    return f - 1.0;                        // Range [0:1]
}

// Pseudo-random value in half-open range [0:1].
float random( vec3  v ) { return floatConstruct(hash(floatBitsToUint(v))); }

bool Move()
{
    const ivec2 position = ivec2(myParticleWindErosion.Position.x, myParticleWindErosion.Position.y);

    if(IsOutOfBounds(position))
    {
        myParticleWindErosion.Sediment = 0.0;
        myParticleWindErosion.Age = 0;
        return false;
    }

    if(myParticleWindErosion.Age > particleWindErosionConfiguration.MaxAge)
    {
        DepositeOnTop(GetIndexVector(position), myParticleWindErosion.Sediment);
        myParticleWindErosion.Sediment = 0.0;
        myParticleWindErosion.Age = 0;
        return false;
    }

    // Compute Movement

    const float height = TotalHeight(GetIndexVector(position));
    if (myParticleWindErosion.Age == 0 || myParticleWindErosion.Position.z < height)
    {
        myParticleWindErosion.Position.z = height;
    }
    vec3 persistentSpeed = vec3(particleWindErosionConfiguration.PersistentSpeed, 0.0);
    const vec3 normal = GetScaledNormal(position.x, position.y);
    const float hfac = exp(-(myParticleWindErosion.Position.z - height) / BoundaryLayer);
    const float shadow = 1.0 - max(0.0, dot(normalize(persistentSpeed), normal));
    const float collision = max(0.0, -dot(normalize(myParticleWindErosion.Speed), normal));
    const vec3 rspeed = cross(normal, cross((1.0 - collision) * myParticleWindErosion.Speed, normal));

    // Apply Base Prevailign Wind-Speed w. Shadowing

    myParticleWindErosion.Speed += 0.05 * ((0.1 + 0.9 * shadow) * persistentSpeed - myParticleWindErosion.Speed);

    // Apply Gravity

    if (myParticleWindErosion.Position.z > height)
    {
        myParticleWindErosion.Speed.z -= particleWindErosionConfiguration.Gravity * myParticleWindErosion.Sediment;
    }

    // Compute Collision Factor

    // Compute Redirect Velocity

    // Speed is accelerated by terrain features

    myParticleWindErosion.Speed += 0.9 * (shadow * mix(persistentSpeed, rspeed, shadow * hfac) - myParticleWindErosion.Speed);

    // Turbulence

    myParticleWindErosion.Speed += 0.1 * hfac * collision * ((random(myParticleWindErosion.Speed) * 1001) - 500.0) / 500.0;

    // Speed is damped by drag

    myParticleWindErosion.Speed *= (1.0 - 0.3 * myParticleWindErosion.Sediment);

    // Move

    myParticleWindErosion.Position += myParticleWindErosion.Speed;

    return true;
}

bool Interact()
{
    // Termination Checks

    const ivec2 currentPosition = ivec2(myParticleWindErosion.Position.x, myParticleWindErosion.Position.y);

    if(IsOutOfBounds(currentPosition))
    {
        return false;
    }

    // Compute Mass Transport
    
    const float height = TotalHeight(GetIndexVector(currentPosition));
    const vec3 normal = GetScaledNormal(currentPosition.x, currentPosition.y);
    const float hfac = exp(-(myParticleWindErosion.Position.z - height) / BoundaryLayer);
    const float collision = max(0.0, -dot(normalize(myParticleWindErosion.Speed), normal));
    const float force = max(0.0, -dot(normalize(myParticleWindErosion.Speed), normal) * length(myParticleWindErosion.Speed));

    float lift = (1.0 - collision) * length(myParticleWindErosion.Speed);

    float capacity = 1.0 * (force * hfac + 0.02 * lift * hfac);

    // Mass Transfer to Equilibrium

    float difference = particleWindErosionConfiguration.SuspensionRate * (capacity - myParticleWindErosion.Sediment);

    if(difference < 0)
    {
        DepositeOnTop(GetIndexVector(currentPosition), abs(difference));
        myParticleWindErosion.Sediment = max(myParticleWindErosion.Sediment + difference, 0.0);
    }
    else
    {
        float suspendedSediment = SuspendFromTop(GetIndexVector(currentPosition), difference);
        myParticleWindErosion.Sediment += suspendedSediment;
    }

    myParticleWindErosion.Age++;

    return true;
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    myHeightMapLength = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(id >= particlesWindErosion.length())
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapLength));

    myParticleWindErosion = particlesWindErosion[id];
    
    if(myParticleWindErosion.Age == 0 && particleWindErosionConfiguration.AreParticlesAdded)
    {
        uint index = windErosionHeightMapIndices[id];
        uint x = index % myHeightMapSideLength;
        uint y = index / myHeightMapSideLength;
        myParticleWindErosion.Age = 0;
        myParticleWindErosion.Sediment = 0.0;
        myParticleWindErosion.Position = vec3(x, y, 0.0);
        myParticleWindErosion.Speed = vec3(0.0, 0.0, 0.0);
    }

    if(Move())
    {
        Interact();
    }

    particlesWindErosion[id] = myParticleWindErosion;

    memoryBarrier();
}