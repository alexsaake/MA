#version 430

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

#define SCALE 60

layout(std430, binding = 1) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

uint myMapSize;

uint getIndex(uint x, uint y)
{
    return (y * myMapSize) + x;
}

uint getIndexV(ivec2 position)
{
    return (position.y * myMapSize) + position.x;
}

bool isOutOfBounds(ivec2 position)
{
    return position.x < 0 || position.x > myMapSize || position.y < 0 || position.y > myMapSize;
}

vec3 getScaledNormal(uint x, uint y)
{
    if (x < 1 || x > myMapSize - 2
        || y < 1 || y > myMapSize - 2)
    {
        return vec3(0.0, 0.0, 1.0);
    }

    float xp1ym1 = heightMap[getIndex(x + 1, y - 1)];
    float xm1ym1 = heightMap[getIndex(x - 1, y - 1)];
    float xp1y = heightMap[getIndex(x + 1, y)];
    float xm1y = heightMap[getIndex(x - 1, y)];
    float xp1yp1 = heightMap[getIndex(x + 1, y + 1)];
    float xm1yp1 = heightMap[getIndex(x - 1, y + 1)];
    float xyp1 = heightMap[getIndex(x, y + 1)];
    float xym1 = heightMap[getIndex(x, y - 1)];

    vec3 normal = vec3(
    SCALE * -(xp1ym1 - xm1ym1 + 2 * (xp1y - xm1y) + xp1yp1 - xm1yp1),
    SCALE * -(xm1yp1 - xm1ym1 + 2 * (xyp1 - xym1) + xp1yp1 - xp1ym1),
    1.0);

    return normalize(normal);
}

const int MaxAge = 1024;
const float EvaporationRate = 0.001;
const float DepositionRate = 0.05;
const float MinimumVolume = 0.001;
const float Entrainment = 10.0;
const float Gravity = 2.0;
vec2 myPosition;
vec2 myOriginalPosition;
vec2 mySpeed;
int myAge;
float myVolume;
float mySediment;

bool Move()
{
    ivec2 position = ivec2(myPosition);

    if(isOutOfBounds(position))
    {
        return false;
    }

    if(myAge > MaxAge)
    {
        return false;
    }

    if(myVolume < MinimumVolume)
    {
        heightMap[getIndexV(position)] += mySediment;
        return false;
    }

    vec3 normal = getScaledNormal(position.x, position.y);

    mySpeed += Gravity * normal.xy / myVolume;

    if(length(mySpeed) > 0)
    {
        mySpeed = sqrt(2.0) * normalize(mySpeed);
    }

    myOriginalPosition = myPosition;
    myPosition += mySpeed;

    return true;
}

bool Interact()
{
    ivec2 position = ivec2(myOriginalPosition);

    if(isOutOfBounds(position))
    {
        return false;
    }

    float h2;
    if(isOutOfBounds(ivec2(myPosition)))
    {
        h2 = 0.99f * heightMap[getIndexV(position)];
    }
    else
    {
        ivec2 currentPosition = ivec2(myPosition);
        h2 = heightMap[getIndexV(currentPosition)];
    }

    float cEq = (1.0 + Entrainment) * heightMap[getIndexV(position)] - h2;
    if(cEq < 0)
    {
        cEq = 0;
    }

    float cDiff = (cEq * myVolume - mySediment);

    float effD = DepositionRate;
    if(effD < 0)
    {
        effD = 0;
    }

    if(effD * cDiff < 0)
    {
        if(effD * cDiff < -mySediment)
        {
            cDiff = -mySediment / effD;
        }
    }

    mySediment += effD * cDiff;
    heightMap[getIndexV(position)] -= effD * cDiff;

    myVolume *= (1.0 - EvaporationRate);

    myAge++;

    return true;
}

//https://github.com/keijiro/ComputePrngTest/blob/master/Assets/Prng.compute
// Hash function from H. Schechter & R. Bridson, goo.gl/RXiKaH
uint Hash(uint s)
{
    s ^= 2747636419u;
    s *= 2654435769u;
    s ^= s >> 16;
    s *= 2654435769u;
    s ^= s >> 16;
    s *= 2654435769u;
    return s;
}

float Random(uint seed)
{
    return float(Hash(seed)) / 4294967295.0; // 2^32-1
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    myMapSize = uint(sqrt(heightMap.length()));

    uint index = uint(Random(id) * heightMap.length());
    uint x = index % myMapSize;
    uint y = index / myMapSize;
    myPosition = vec2(x, y);
    myOriginalPosition = vec2(0.0, 0.0);
    mySpeed = vec2(0.0, 0.0);
    myAge = 0;
    myVolume = 1.0;
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