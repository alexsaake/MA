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
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

uint myHeightMapPlaneSize;

bool IsFloorIndex(uint index)
{
    uint tempIndex = index - mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize;
    return tempIndex >= 0 && tempIndex < myHeightMapPlaneSize;
}

uint LayerHeightMapOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

float FineSedimentHeight(uint index, uint layer)
{
    return heightMap[index + (mapGenerationConfiguration.RockTypeCount - 1) * myHeightMapPlaneSize + LayerHeightMapOffset(layer)];
}

float CoarseSedimentHeight(uint index, uint layer)
{
    return heightMap[index + 1 * myHeightMapPlaneSize + LayerHeightMapOffset(layer)];
}

float BedrockHeight(uint index, uint layer)
{
    return heightMap[index + LayerHeightMapOffset(layer)];
}

float LayerHeightMapFloorHeight(uint index, uint layer)
{
    if(layer < 1)
    {
        return 0.0;
    }
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

float LayerHeightMapHeight(uint index, uint totalIndex, uint layer)
{
    float height = 0.0;
    float heightMapFloorHeight = 0.0;
    if(layer > 0)
    {
        heightMapFloorHeight = LayerHeightMapFloorHeight(index, layer);
            if(heightMapFloorHeight == 0)
            {
                return 0.0;
            }
    }
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        uint currentRockTypeIndex = index + rockType * myHeightMapPlaneSize + LayerHeightMapOffset(layer);
        if(totalIndex < currentRockTypeIndex)
        {
            return heightMapFloorHeight + height;
        }
        height += heightMap[currentRockTypeIndex];
    }
    return heightMapFloorHeight + height;
}

in vec3 vertexPosition;
in vec4 vertexColor;
in vec3 vertexNormal;
in vec2 vertexTexCoords;

uniform mat4 mvp;
uniform mat4 matModel;

out vec3 fragPosition;
out vec4 fragColor;
out vec3 fragNormal;

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
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);

    uint heightMapIndex = uint(vertexTexCoords.x);
    uint layerOffset = (mapGenerationConfiguration.RockTypeCount + 1) * myHeightMapPlaneSize;
    uint layer = uint(heightMapIndex * 1.0 / layerOffset);
    uint rockTypeIndex = heightMapIndex - LayerHeightMapOffset(layer);
    uint rockType = uint(rockTypeIndex * 1.0 / myHeightMapPlaneSize);
    uint baseIndex = rockTypeIndex - rockType * myHeightMapPlaneSize;
    float height = 0.0;
    if(heightMapIndex > 0)
    {
        if(IsFloorIndex(heightMapIndex))
        {
            height = heightMap[heightMapIndex];
        }
        else
        {
            height = LayerHeightMapHeight(baseIndex, heightMapIndex, layer);
        }
    }
    float terrainHeight = height * mapGenerationConfiguration.HeightMultiplier;
    float seaLevelHeight = mapGenerationConfiguration.SeaLevel * mapGenerationConfiguration.HeightMultiplier;
    fragPosition = vec3(matModel * vec4(vertexPosition.xy, terrainHeight, 1.0));
    fragNormal = transpose(inverse(mat3(matModel))) * vertexNormal;
    vec3 normal = vertexNormal;
    vec3 terrainColor = vec3(1.0);
    if(mapGenerationConfiguration.AreTerrainColorsEnabled)
    {
        if(mapGenerationConfiguration.RockTypeCount > 1)
        {
            uint cubeFace = uint(vertexTexCoords.y);
            if(cubeFace == 1)
            {
                if(FineSedimentHeight(baseIndex, layer) > 0.00001)
                {
                    terrainColor = fineSedimentColor;
                }
                else if(mapGenerationConfiguration.RockTypeCount > 2
                    && CoarseSedimentHeight(baseIndex, layer) > 0.00001)
                {
                    terrainColor = coarseSedimentColor;
                }
                else
                {
                    terrainColor = bedrockColor;
                }
            }
            else if(cubeFace == 2)
            {
                if(BedrockHeight(baseIndex, layer) > 0)
                {
                    terrainColor = bedrockColor;
                }
                else if(mapGenerationConfiguration.RockTypeCount > 2
                    && CoarseSedimentHeight(baseIndex, layer) > 0.00001)
                {
                    terrainColor = coarseSedimentColor;
                }
                else if(FineSedimentHeight(baseIndex, layer) > 0.00001)
                {
                    terrainColor = fineSedimentColor;
                }
            }
            else
            {
                terrainColor = vertexColor.rgb;
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