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
    float IsColorEnabled;
};

layout(std430, binding = 3) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
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
const vec3 persistentSpeed = vec3(0.0, 0.125, 0.0);
const uint MaxAge = 102400;
const float BoundaryLayer = 2.0;
const float Suspension = 0.05;
const float Gravity = 0.025;
const float MaxDiff = 0.005;
const float Settling = 0.25;
vec3 myPosition;
vec3 mySpeed;
int myAge;
float mySediment;

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
        excess = abs(diff) - sn[i].d * MaxDiff;
        if (excess <= 0)
        {
            continue;
        }

        // Actual Amount Transferred
        float transfer = Settling * excess / 2.0;
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
    const ivec2 position = ivec2(myPosition.x, myPosition.y);

    if(isOutOfBounds(position))
    {
        return false;
    }

    if(myAge > MaxAge)
    {
        heightMap[getIndexV(position)] += mySediment;
        Cascade(position);
        return false;
    }

    // Compute Movement

    const float height = heightMap[getIndexV(position)];
    if (myAge == 0 || myPosition.z < height)
    {
        myPosition.z = height;
    }    
    const vec3 normal = getScaledNormal(position.x, position.y);
    const float hfac = exp(-(myPosition.z - height) / BoundaryLayer);
    const float shadow = 1.0 - max(0.0, dot(normalize(persistentSpeed), normal));
    const float collision = max(0.0, -dot(normalize(mySpeed), normal));
    const vec3 rspeed = cross(normal, cross((1.0 - collision) * mySpeed, normal));

    // Apply Base Prevailign Wind-Speed w. Shadowing

    mySpeed += 0.05 * ((0.1 + 0.9 * shadow) * persistentSpeed - mySpeed);

    // Apply Gravity

    if (myPosition.z > height)
    {
        mySpeed.z -= Gravity * mySediment;
    }

    // Compute Collision Factor

    // Compute Redirect Velocity

    // Speed is accelerated by terrain features

    mySpeed += 0.9 * (shadow * mix(persistentSpeed, rspeed, shadow * hfac) - mySpeed);

    // Turbulence

    mySpeed += 0.1 * hfac * collision * ((random(mySpeed) * 1001) - 500.0) / 500.0;

    // Speed is damped by drag

    mySpeed *= (1.0 - 0.3 * mySediment);

    // Move

    myPosition += mySpeed;

    return true;
}

bool Interact()
{
    // Termination Checks

    const ivec2 currentPosition = ivec2(myPosition.x, myPosition.y);

    if(isOutOfBounds(currentPosition))
    {
        return false;
    }

    // Compute Mass Transport
    
    const float height = heightMap[getIndexV(currentPosition)];
    const vec3 normal = getScaledNormal(currentPosition.x, currentPosition.y);
    const float hfac = exp(-(myPosition.z - height) / BoundaryLayer);
    const float collision = max(0.0, -dot(normalize(mySpeed), normal));
    const float force = max(0.0, -dot(normalize(mySpeed), normal) * length(mySpeed));

    float lift = (1.0 - collision) * length(mySpeed);

    float capacity = 1 * (force * hfac + 0.02 * lift * hfac);

    // Mass Transfer to Equilibrium

    float diff = capacity - mySediment;

    mySediment += Suspension * diff;
    heightMap[getIndexV(currentPosition)] -= Suspension * diff;

    Cascade(currentPosition);

    myAge++;

    return true;
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint index = heightMapIndices[id];
    uint x = index % myHeightMapSideLength;
    uint y = index / myHeightMapSideLength;
    myPosition = vec3(x, y, 0);
    mySpeed = vec3(0.0, 0.0, 0.0);
    myAge = 0;
    mySediment = 0.0;

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