#version 430

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
    bool AreLayerColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

struct ErosionConfiguration
{
    float DeltaTime;
	bool IsWaterKeptInBoundaries;
};

layout(std430, binding = 6) readonly restrict buffer erosionConfigurationShaderBuffer
{
    ErosionConfiguration erosionConfiguration;
};

struct PlateTectonicsSegment
{
    int Plate;
    float Mass;
    float Inertia;
    float Density;
    float Height;
    float Thickness;
    bool IsAlive;
    bool IsColliding;
    vec2 Position;
};

layout(std430, binding = 15) buffer plateTectonicsSegmentsShaderBuffer
{
    PlateTectonicsSegment[] plateTectonicsSegments;
};

uint myHeightMapSideLength;
uint myHeightMapPlaneSize;

uint HeightMapLayerOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

uint HeightMapRockTypeOffset(uint rockType)
{
    return rockType * myHeightMapPlaneSize;
}

float TotalFineSedimentHeight(uint index)
{
    float height = 0;
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        height += heightMap[index + (mapGenerationConfiguration.RockTypeCount - 1) * myHeightMapPlaneSize + HeightMapLayerOffset(uint(layer))];
    }
    return height;
}

float TotalCoarseSedimentHeight(uint index)
{
    float height = 0;
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        height += heightMap[index + 1 * myHeightMapPlaneSize + HeightMapLayerOffset(uint(layer))];
    }
    return height;
}

float HeightMapLayerFloorHeight(uint index, uint layer)
{
    if(layer < 1
        || layer >= mapGenerationConfiguration.LayerCount)
    {
        return 0.0;
    }
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

float HeightMapLayerHeight(uint index, uint layer)
{
    float heightMapLayerHeight = 0.0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        heightMapLayerHeight += heightMap[index + HeightMapRockTypeOffset(rockType) + HeightMapLayerOffset(layer)];
    }
    return heightMapLayerHeight;
}

float TotalHeightMapHeight(uint index)
{
    float heightMapLayerFloorHeight = 0.0;
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        heightMapLayerFloorHeight = 0.0;
        if(layer > 0)
        {
            heightMapLayerFloorHeight = HeightMapLayerFloorHeight(index, uint(layer));
            if(heightMapLayerFloorHeight == 0)
            {
                continue;
            }
        }
        float heightMapLayerHeight = HeightMapLayerHeight(index, uint(layer));
        if(heightMapLayerHeight > 0)
        {
            return heightMapLayerFloorHeight + heightMapLayerHeight;
        }
    }
    return 0.0;
}

uint GetIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

vec3 GetScaledNormal(uint x, uint y)
{
    if (x < 1 || x > myHeightMapSideLength - 2
        || y < 1 || y > myHeightMapSideLength - 2)
    {
        return vec3(0.0, 0.0, 1.0);
    }

    float rb = TotalHeightMapHeight(GetIndex(x + 1, y - 1));
    float lb = TotalHeightMapHeight(GetIndex(x - 1, y - 1));
    float r = TotalHeightMapHeight(GetIndex(x + 1, y));
    float l = TotalHeightMapHeight(GetIndex(x - 1, y));
    float rt = TotalHeightMapHeight(GetIndex(x + 1, y + 1));
    float lt = TotalHeightMapHeight(GetIndex(x - 1, y + 1));
    float t = TotalHeightMapHeight(GetIndex(x, y + 1));
    float b = TotalHeightMapHeight(GetIndex(x, y - 1));

    vec3 normal = vec3(
    mapGenerationConfiguration.HeightMultiplier * -(rb - lb + 2 * (r - l) + rt - lt),
    mapGenerationConfiguration.HeightMultiplier * -(lt - lb + 2 * (t - b) + rt - rb),
    1.0);

    return normalize(normal);
}

in vec3 vertexPosition;

uniform mat4 mvp;
uniform mat4 matModel;

out vec3 fragPosition;
out vec3 fragNormal;
out vec4 fragColor;

vec3 oceanCliff = vec3(0.2, 0.2, 0.1);
vec3 beachColor = vec3(1.0, 0.9, 0.6);
vec3 pastureColor = vec3(0.5, 0.6, 0.4);
vec3 woodsColor = vec3(0.2, 0.3, 0.2);
vec3 mountainColor = vec3(0.6, 0.6, 0.6);
vec3 snowColor = vec3(1.0, 0.9, 0.9);

vec3 bedrockColor = mountainColor;
vec3 coarseSedimentColor = vec3(0.5, 0.3, 0.3);
vec3 fineSedimentColor = beachColor;

void main()
{
    uint index = gl_VertexID;
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= myHeightMapPlaneSize)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapPlaneSize));

    uint x = index % myHeightMapSideLength;
    uint y = index / myHeightMapSideLength;
    
    float height = TotalHeightMapHeight(index);
    float terrainHeight = height * mapGenerationConfiguration.HeightMultiplier;
    float seaLevelHeight = mapGenerationConfiguration.SeaLevel * mapGenerationConfiguration.HeightMultiplier;
    fragPosition = vec3(matModel * vec4(vertexPosition.xy, terrainHeight, 1.0));
    vec3 normal = GetScaledNormal(x, y);
    fragNormal = transpose(inverse(mat3(matModel))) * normal;
    
    vec3 terrainColor = vec3(1.0);    
    if(mapGenerationConfiguration.LayerCount > 1
        && mapGenerationConfiguration.AreLayerColorsEnabled)
    {
        uint layer = 0;
        if(HeightMapLayerFloorHeight(index, 1) > 0)
        {
            layer = 1;
        }
        if(layer == 0)
        {
            terrainColor = vec3(1, 0, 0);
        }
        if(layer == 1)
        {
            terrainColor = vec3(0, 1, 0);
        }
    }
    else if(plateTectonicsSegments.length() > 0
        && mapGenerationConfiguration.ArePlateTectonicsPlateColorsEnabled)
    {
        int plate = plateTectonicsSegments[index].Plate;
        if(plate == 0)
        {
            terrainColor = vec3(1, 0, 0);
        }
        if(plate == 1)
        {
            terrainColor = vec3(0, 1, 0);
        }
        if(plate == 2)
        {
            terrainColor = vec3(0, 0, 1);
        }
        if(plate == 3)
        {
            terrainColor = vec3(1, 0, 1);
        }
        if(plate == 4)
        {
            terrainColor = vec3(0, 1, 1);
        }
        if(plate == 5)
        {
            terrainColor = vec3(1, 1, 0);
        }
        if(plate == 6)
        {
            terrainColor = vec3(0.5, 0, 0);
        }
        if(plate == 7)
        {
            terrainColor = vec3(0, 0.5, 0);
        }
        if(plate == 8)
        {
            terrainColor = vec3(0, 0, 0.5);
        }
        if(plate == 9)
        {
            terrainColor = vec3(0.5, 0, 0.5);
        }
    }
    else if(mapGenerationConfiguration.AreTerrainColorsEnabled)
    {
        if(mapGenerationConfiguration.RockTypeCount > 1)
        {
            if(TotalFineSedimentHeight(index) > 0.00001)
            {
                terrainColor = fineSedimentColor;
            }
            else if(mapGenerationConfiguration.RockTypeCount > 2
                && TotalCoarseSedimentHeight(index) > 0.00001)
            {
                terrainColor = coarseSedimentColor;
            }
            else
            {
                terrainColor = bedrockColor;
            }
        }
        else
        {
            if(terrainHeight < seaLevelHeight + 0.3)
            {
                if(normal.z > 0.3)
                {
                    terrainColor = beachColor;
                }
                else
                {
                    terrainColor = oceanCliff;
                }
            }
            else
            {
                if(normal.z > 0.4)
                {
                    if(height > 0.9)
                    {
                        terrainColor = snowColor;
                    }
                    else if(height > 0.7)
                    {
                        terrainColor = mountainColor;
                    }
                    else
                    {
                        terrainColor = woodsColor;
                    }
                }
                else if(normal.z > 0.3)
                {
                    if(height > 0.9)
                    {
                        terrainColor = snowColor;
                    }
                    else if(height > 0.8)
                    {
                        terrainColor = mountainColor;
                    }
                    else
                    {
                        terrainColor = pastureColor;
                    }
                }
                else
                {
                    terrainColor = mountainColor;
                }
            }
        }
    }
    fragColor = vec4(terrainColor, 1.0);
    gl_Position = mvp * vec4(vertexPosition.xy, terrainHeight, 1.0);
}