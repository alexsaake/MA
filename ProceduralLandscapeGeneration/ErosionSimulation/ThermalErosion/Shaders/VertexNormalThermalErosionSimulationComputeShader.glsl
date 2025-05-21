#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
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

struct ThermalErosionConfiguration
{
    float ErosionRate;
};

layout(std430, binding = 10) readonly restrict buffer thermalErosionConfigurationShaderBuffer
{
    ThermalErosionConfiguration thermalErosionConfiguration;
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

uint GetIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

//https://aparis69.github.io/public_html/posts/terrain_erosion.html
uint myHeightMapLength;

float TangensAngleOfRepose(uint index)
{
	for(int rockType = int(mapGenerationConfiguration.RockTypeCount) - 1; rockType >= 0; rockType--)
	{
		if(heightMap[index + rockType * myHeightMapLength] > 0)
		{
			return rockTypesConfiguration[rockType].TangensAngleOfRepose;
		}
	}
	return rockTypesConfiguration[0].TangensAngleOfRepose;
}

void RemoveFromTop(uint index, float sediment)
{
    for(int rockType = int(mapGenerationConfiguration.RockTypeCount) - 1; rockType >= 0; rockType--)
    {
        uint offsetIndex = index + rockType * myHeightMapLength;
        float height = heightMap[offsetIndex];
        if(height > 0)
        {
            if(height >= sediment)
            {
                heightMap[offsetIndex] -= sediment;
            }
            else
            {
                heightMap[offsetIndex] = 0;
            }
        }
    }
}

void DepositeOnTop(uint index, float sediment)
{
    heightMap[index + (mapGenerationConfiguration.RockTypeCount - 1) * myHeightMapLength] += sediment;
}

float TotalHeight(uint index)
{
    float height = 0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapLength];
    }
    return height;
}

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

void main()
{
    uint index = gl_GlobalInvocationID.x;
    myHeightMapLength = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= myHeightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapLength));

    uint x = index % myHeightMapSideLength;
    uint y = index / myHeightMapSideLength;

    vec2 normal = GetScaledNormal(x, y).xy;
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
    uint neighborIndex = GetIndex(x + int(normal.x), y + int(normal.y));
    float distance = length(normal);
    if(index == neighborIndex
        || neighborIndex < 0
        || neighborIndex > myHeightMapLength)
    {
        return;
    }
    float heightDifference = TotalHeight(index) - TotalHeight(neighborIndex);
	float tangensAngle = heightDifference * mapGenerationConfiguration.HeightMultiplier / 1.0 / distance;
    if (heightDifference < 0
        || tangensAngle < TangensAngleOfRepose(index))
    {
        return;
    }

    float heightChange = heightDifference * thermalErosionConfiguration.ErosionRate * erosionConfiguration.TimeDelta;

    RemoveFromTop(index, heightChange);
    DepositeOnTop(neighborIndex, heightChange);
        
    memoryBarrier();
}