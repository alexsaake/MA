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

struct ParticleWindErosionConfiguration
{
    float Suspension;
    float Gravity;
    float MaxDiff;
    float Settling;
    uint MaxAge;
    vec2 PersistentSpeed;
    bool AreParticlesAdded;
    bool AreParticlesDisplayed;
};

layout(std430, binding = 4) readonly restrict buffer particleWindErosionConfigurationShaderBuffer
{
    ParticleWindErosionConfiguration particleWindErosionConfiguration;
};

struct ParticleWindErosion
{
    int Age;
    float Sediment;
    vec3 Position;
    vec3 Speed;
};

layout(std430, binding = 5) buffer particleWindErosionShaderBuffer
{
    ParticleWindErosion[] particlesWindErosion;
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

//https://github.com/erosiv/soillib/blob/main/source/particle/wind.hpp
ParticleWindErosion myParticleWindErosion;
const float BoundaryLayer = 2.0;

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
        excess = abs(diff) - sn[i].d * particleWindErosionConfiguration.MaxDiff;
        if (excess <= 0)
        {
            continue;
        }

        // Actual Amount Transferred
        float transfer = particleWindErosionConfiguration.Settling * excess / 2.0;
        heightMap[tindex] -= transfer;
        heightMap[bindex] += transfer;
    }
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

    if(isOutOfBounds(position))
    {
        myParticleWindErosion.Sediment = 0.0;
        myParticleWindErosion.Age = 0;
        return false;
    }

    if(myParticleWindErosion.Age > particleWindErosionConfiguration.MaxAge)
    {
        heightMap[getIndexV(position)] += myParticleWindErosion.Sediment;
        myParticleWindErosion.Sediment = 0.0;
        myParticleWindErosion.Age = 0;
        Cascade(position);
        return false;
    }

    // Compute Movement

    const float height = heightMap[getIndexV(position)];
    if (myParticleWindErosion.Age == 0 || myParticleWindErosion.Position.z < height)
    {
        myParticleWindErosion.Position.z = height;
    }
    vec3 persistentSpeed = vec3(particleWindErosionConfiguration.PersistentSpeed, 0.0);
    const vec3 normal = getScaledNormal(position.x, position.y);
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

    if(isOutOfBounds(currentPosition))
    {
        return false;
    }

    // Compute Mass Transport
    
    const float height = heightMap[getIndexV(currentPosition)];
    const vec3 normal = getScaledNormal(currentPosition.x, currentPosition.y);
    const float hfac = exp(-(myParticleWindErosion.Position.z - height) / BoundaryLayer);
    const float collision = max(0.0, -dot(normalize(myParticleWindErosion.Speed), normal));
    const float force = max(0.0, -dot(normalize(myParticleWindErosion.Speed), normal) * length(myParticleWindErosion.Speed));

    float lift = (1.0 - collision) * length(myParticleWindErosion.Speed);

    float capacity = 1 * (force * hfac + 0.02 * lift * hfac);

    // Mass Transfer to Equilibrium

    float diff = capacity - myParticleWindErosion.Sediment;

    myParticleWindErosion.Sediment += particleWindErosionConfiguration.Suspension * diff;
    heightMap[getIndexV(currentPosition)] -= particleWindErosionConfiguration.Suspension * diff;

    Cascade(currentPosition);

    myParticleWindErosion.Age++;

    return true;
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    if(id >= particlesWindErosion.length())
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(heightMap.length()));

    myParticleWindErosion = particlesWindErosion[id];
    
    if(myParticleWindErosion.Age == 0 && particleWindErosionConfiguration.AreParticlesAdded)
    {
        uint index = heightMapIndices[id];
        uint x = index % myHeightMapSideLength;
        uint y = index / myHeightMapSideLength;
        myParticleWindErosion.Age = 0;
        myParticleWindErosion.Sediment = 0.0;
        myParticleWindErosion.Position = vec3(x, y, 0.0);
        myParticleWindErosion.Speed = vec3(0.0, 0.0, 0.0);
    }

    if(particleWindErosionConfiguration.AreParticlesDisplayed)
    {
        if(Move())
        {
            Interact();
        }
    }
    else
    {
        while(true)
        {
            if(!Move())
            {
                break;
            }
            if(!Interact())
            {
                break;
            }
        }
    }

    particlesWindErosion[id] = myParticleWindErosion;
}