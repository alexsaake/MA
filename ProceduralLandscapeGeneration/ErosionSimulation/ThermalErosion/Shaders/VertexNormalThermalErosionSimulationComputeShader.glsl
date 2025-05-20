#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    uint LayerCount;
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
    float ErosionRate;
};

layout(std430, binding = 10) readonly restrict buffer thermalErosionConfigurationShaderBuffer
{
    ThermalErosionConfiguration thermalErosionConfiguration;
};

struct LayersConfiguration
{
    float Hardness;
    float TangensAngleOfRepose;
};

layout(std430, binding = 18) buffer layersConfigurationShaderBuffer
{
    LayersConfiguration[] layersConfiguration;
};

uint myHeightMapSideLength;

uint getIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

//https://aparis69.github.io/public_html/posts/terrain_erosion.html
uint myHeightMapLength;

float HeightTopmostLayer(uint index)
{
	for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
	{
        float height = heightMap[index + layer * myHeightMapLength];
		if(height > 0)
		{
			return height;
		}
	}
	return 0;   
}

float TangensAngleOfRepose(uint index)
{
	for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
	{
		if(heightMap[index + layer * myHeightMapLength] > 0)
		{
			return layersConfiguration[layer].TangensAngleOfRepose;
		}
	}
	return layersConfiguration[0].TangensAngleOfRepose;
}

float RemoveFromTop(uint index, float sediment)
{
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        uint offsetIndex = index + layer * myHeightMapLength;
        float height = heightMap[offsetIndex];
        if(height > 0)
        {
            if(height > sediment)
            {
                heightMap[offsetIndex] -= sediment;
                return sediment;
            }
            else
            {
                heightMap[offsetIndex] = 0;
                return sediment - height;
            }
        }
    }
    return 0;
}

void DepositeOnTop(uint index, float sediment)
{
    heightMap[index + (mapGenerationConfiguration.LayerCount - 1) * myHeightMapLength] += sediment;
}

float TotalHeight(uint index)
{
    float height = 0;
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        height += heightMap[index + layer * myHeightMapLength];
    }
    return height;
}

vec3 getScaledNormal(uint x, uint y)
{
    if (x < 1 || x > myHeightMapSideLength - 2
        || y < 1 || y > myHeightMapSideLength - 2)
    {
        return vec3(0.0, 0.0, 1.0);
    }
    
    float rb = TotalHeight(getIndex(x + 1, y - 1));
    float lb = TotalHeight(getIndex(x - 1, y - 1));
    float r = TotalHeight(getIndex(x + 1, y));
    float l = TotalHeight(getIndex(x - 1, y));
    float rt = TotalHeight(getIndex(x + 1, y + 1));
    float lt = TotalHeight(getIndex(x - 1, y + 1));
    float t = TotalHeight(getIndex(x, y + 1));
    float b = TotalHeight(getIndex(x, y - 1));

    vec3 normal = vec3(
    mapGenerationConfiguration.HeightMultiplier * -(rb - lb + 2 * (r - l) + rt - lt),
    mapGenerationConfiguration.HeightMultiplier * -(lt - lb + 2 * (t - b) + rt - rb),
    1.0);

    return normalize(normal);
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    myHeightMapLength = heightMap.length() / mapGenerationConfiguration.LayerCount;
    if(id >= myHeightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapLength));

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
    if(id == neighborIndex
        || neighborIndex < 0
        || neighborIndex > myHeightMapLength)
    {
        return;
    }
    float heightDifference = TotalHeight(id) - TotalHeight(neighborIndex);
	float tangensAngle = heightDifference * mapGenerationConfiguration.HeightMultiplier / 1.0;
    if (heightDifference < 0
        || tangensAngle < TangensAngleOfRepose(id))
    {
        return;
    }

    float heightTopmostLayer = HeightTopmostLayer(id);
    float heightChange = min(heightDifference, heightTopmostLayer) * thermalErosionConfiguration.ErosionRate * erosionConfiguration.TimeDelta;

    float removedSediment = RemoveFromTop(id, heightChange);
    DepositeOnTop(neighborIndex, removedSediment);
        
    memoryBarrier();
}