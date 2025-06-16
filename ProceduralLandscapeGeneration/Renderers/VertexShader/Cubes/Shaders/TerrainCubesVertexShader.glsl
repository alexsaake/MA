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

uint myHeightMapPlaneSize;

bool IsFloorIndex(uint index)
{
    uint tempIndex = index - mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize;
    return tempIndex >= 0 && tempIndex < myHeightMapPlaneSize;
}

uint HeightMapLayerOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

uint HeightMapRockTypeOffset(uint rockType)
{
    return rockType * myHeightMapPlaneSize;
}

float FineSedimentHeight(uint index, uint layer)
{
    return heightMap[index + (mapGenerationConfiguration.RockTypeCount - 1) * myHeightMapPlaneSize + HeightMapLayerOffset(layer)];
}

float CoarseSedimentHeight(uint index, uint layer)
{
    return heightMap[index + 1 * myHeightMapPlaneSize + HeightMapLayerOffset(layer)];
}

float BedrockHeight(uint index, uint layer)
{
    return heightMap[index + HeightMapLayerOffset(layer)];
}

float HeightMapLayerFloorHeight(uint index, uint layer)
{
    if(layer < 1)
    {
        return 0.0;
    }
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

float HeightMapLayerHeight(uint index, uint totalIndex, uint layer)
{
    float height = 0.0;
    float heightMapFloorHeight = 0.0;
    if(layer > 0)
    {
        heightMapFloorHeight = HeightMapLayerFloorHeight(index, layer);
            if(heightMapFloorHeight == 0)
            {
                return 0.0;
            }
    }
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        uint currentRockTypeIndex = index + HeightMapRockTypeOffset(rockType) + HeightMapLayerOffset(layer);
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
    uint rockTypeIndex = heightMapIndex - HeightMapLayerOffset(layer);
    uint rockType = uint(rockTypeIndex * 1.0 / myHeightMapPlaneSize);
    uint baseIndex = rockTypeIndex - HeightMapRockTypeOffset(rockType);
    float height = 0.0;
    if(heightMapIndex > 0)
    {
        if(IsFloorIndex(heightMapIndex))
        {
            height = heightMap[heightMapIndex];
            layer = 1;
        }
        else
        {
            height = HeightMapLayerHeight(baseIndex, heightMapIndex, layer);
        }
    }
    float terrainHeight = height * mapGenerationConfiguration.HeightMultiplier;
    float seaLevelHeight = mapGenerationConfiguration.SeaLevel * mapGenerationConfiguration.HeightMultiplier;
    fragPosition = vec3(matModel * vec4(vertexPosition.xy, terrainHeight, 1.0));
    fragNormal = transpose(inverse(mat3(matModel))) * vertexNormal;
    vec3 normal = vertexNormal;
    vec3 terrainColor = vec3(1.0);
    if(mapGenerationConfiguration.LayerCount > 1
        && mapGenerationConfiguration.AreLayerColorsEnabled)
    {
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
        int plate = plateTectonicsSegments[baseIndex].Plate;
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