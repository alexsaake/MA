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
    float TangensTalusAngle;
    float ErosionRate;
    float Dampening;
};

layout(std430, binding = 10) readonly restrict buffer thermalErosionConfigurationShaderBuffer
{
    ThermalErosionConfiguration thermalErosionConfiguration;
};

uint myHeightMapSideLength;

uint getIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

//https://aparis69.github.io/public_html/posts/terrain_erosion.html

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

    vec2 normal = getScaledNormal(x, y).xy;
    float tolerance = 0.3;
    if(normal.x > 0)
    {
        if(normal.x < tolerance)
        {
            normal.x = 0;
        }
        else
        {
            normal.x = 1;
        }
    }
    else if(normal.x < 0)
    {
        if(normal.x > -tolerance)
        {
            normal.x = 0;
        }
        else
        {
            normal.x = -1;
        }
    }
    if(normal.y > 0)
    {
        if(normal.y < tolerance)
        {
            normal.y = 0;
        }
        else
        {
            normal.y = 1;
        }
    }
    else if(normal.y < 0)
    {
        if(normal.y > -tolerance)
        {
            normal.y = 0;
        }
        else
        {
            normal.y = -1;
        }
    }
    uint neighborIndex = getIndex(x + int(normal.x), y + int(normal.y));
    if(neighborIndex < 0 || neighborIndex > heightMap.length())
    {
        return;
    }
    float heightDifference = heightMap[id] - heightMap[neighborIndex];
	float tangensAngle = heightDifference * mapGenerationConfiguration.HeightMultiplier / 1.0;

    if (tangensAngle > thermalErosionConfiguration.TangensTalusAngle)
    {
        float heightChange = heightDifference * thermalErosionConfiguration.ErosionRate * erosionConfiguration.TimeDelta * (1.0 - thermalErosionConfiguration.Dampening);
        heightMap[id] -= heightChange;
        heightMap[neighborIndex] += heightChange;
        
        memoryBarrier();
    }
}