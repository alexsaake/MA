#version 430

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 1) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct ThermalErosionConfiguration
{
    uint heightMultiplier;
    float tangensThresholdAngle;
    float heightChange;
};

layout(std430, binding = 2) readonly restrict buffer configurationShaderBuffer
{
    ThermalErosionConfiguration configuration;
};

uint myHeightMapSideLength;

uint getIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

bool isOutOfBounds(uint x, uint y)
{
    return x < 0 || x > myHeightMapSideLength || y < 0 || y > myHeightMapSideLength;
}

//https://aparis69.github.io/public_html/posts/terrain_erosion.html

vec3 getScaledNormal(uint x, uint y)
{
    if (x < 1 || x > myHeightMapSideLength - 2
        || y < 1 || y > myHeightMapSideLength - 2)
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
    configuration.heightMultiplier * -(xp1ym1 - xm1ym1 + 2 * (xp1y - xm1y) + xp1yp1 - xm1yp1),
    configuration.heightMultiplier * -(xm1yp1 - xm1ym1 + 2 * (xyp1 - xym1) + xp1yp1 - xp1ym1),
    1.0);

    return normalize(normal);
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    if(id > heightMap.length())
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;

    vec2 normal = getScaledNormal(x, y).xy;
    if(normal.x > 0)
    {
        normal.x = ceil(normal.x);
    }
    else
    {
        normal.x = floor(normal.x);
    }
    if(normal.y > 0)
    {
        normal.y = ceil(normal.y);
    }
    else
    {
        normal.y = floor(normal.y);
    }
    uint neighborIndex = getIndex(x + int(normal.x), y + int(normal.y));
    if(neighborIndex < 0 || neighborIndex > heightMap.length())
    {
        return;
    }
    float neighborHeight = heightMap[neighborIndex] * configuration.heightMultiplier;
    float zDiff = heightMap[id] * configuration.heightMultiplier - neighborHeight;

    if (zDiff > configuration.tangensThresholdAngle)
    {
        heightMap[id] -= configuration.heightChange;
        heightMap[neighborIndex] += configuration.heightChange;
    }
}