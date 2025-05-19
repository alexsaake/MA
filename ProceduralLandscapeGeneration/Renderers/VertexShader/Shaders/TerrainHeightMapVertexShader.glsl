#version 430

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
uint myHeightMapLength;

float SedimentHeight(uint index)
{
    return heightMap[index + (mapGenerationConfiguration.LayerCount - 1) * myHeightMapLength];
}

float ClayHeight(uint index)
{
    return heightMap[index + 1 * myHeightMapLength];
}

float totalHeight(uint index)
{
    float height = 0;
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        height += heightMap[index + layer * myHeightMapLength];
    }
    return height;
}

uint getIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

vec3 getScaledNormal(uint x, uint y)
{
    if (x < 1 || x > myHeightMapSideLength - 2
        || y < 1 || y > myHeightMapSideLength - 2)
    {
        return vec3(0.0, 0.0, 1.0);
    }

    float rb = totalHeight(getIndex(x + 1, y - 1));
    float lb = totalHeight(getIndex(x - 1, y - 1));
    float r = totalHeight(getIndex(x + 1, y));
    float l = totalHeight(getIndex(x - 1, y));
    float rt = totalHeight(getIndex(x + 1, y + 1));
    float lt = totalHeight(getIndex(x - 1, y + 1));
    float t = totalHeight(getIndex(x, y + 1));
    float b = totalHeight(getIndex(x, y - 1));

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
vec3 clayColor = vec3(0.5, 0.3, 0.3);
vec3 beachColor = vec3(1.0, 0.9, 0.6);
vec3 pastureColor = vec3(0.5, 0.6, 0.4);
vec3 woodsColor = vec3(0.2, 0.3, 0.2);
vec3 mountainColor = vec3(0.6, 0.6, 0.6);
vec3 snowColor = vec3(1.0, 0.9, 0.9);

void main()
{
    uint index = gl_VertexID;
    myHeightMapLength = heightMap.length() / mapGenerationConfiguration.LayerCount;
    if(index >= myHeightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapLength));

    uint x = index % myHeightMapSideLength;
    uint y = index / myHeightMapSideLength;
    
    float height = totalHeight(index);
    float terrainHeight = height * mapGenerationConfiguration.HeightMultiplier;
    float seaLevelHeight = erosionConfiguration.SeaLevel * mapGenerationConfiguration.HeightMultiplier;
    fragPosition = vec3(matModel * vec4(vertexPosition.xy, terrainHeight, 1.0));
    vec3 normal = getScaledNormal(x, y);
    fragNormal = transpose(inverse(mat3(matModel))) * normal;
    
    vec3 terrainColor = vec3(1.0);
    if(plateTectonicsSegments.length() > 0
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
        if(mapGenerationConfiguration.LayerCount > 2)
        {
            if(SedimentHeight(index) > 0.00001)
            {
                terrainColor = beachColor;
            }
            else if(ClayHeight(index) > 0.00001)
            {
                terrainColor = clayColor;
            }
            else
            {
                terrainColor = mountainColor;
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