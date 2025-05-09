#version 430

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    bool IsColorEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

struct ErosionConfiguration
{
    float SeaLevel;
};

layout(std430, binding = 6) readonly restrict buffer erosionConfigurationShaderBuffer
{
    ErosionConfiguration erosionConfiguration;
};

uint myHeightMapSideLength;

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

in vec3 vertexPosition;

uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 lightSpaceMatrix;

out vec3 fragPosition;
out vec3 fragNormal;
out vec4 fragColor;
out vec4 fragPosLightSpace;

vec3 oceanCliff = vec3(0.2, 0.2, 0.1);
vec3 beachColor = vec3(1.0, 0.9, 0.6);
vec3 pastureColor = vec3(0.5, 0.6, 0.4);
vec3 woodsColor = vec3(0.2, 0.3, 0.2);
vec3 mountainColor = vec3(0.6, 0.6, 0.6);
vec3 snowColor = vec3(1.0, 0.9, 0.9);

void main()
{
    uint index = gl_VertexID;
    myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint x = index % myHeightMapSideLength;
    uint y = index / myHeightMapSideLength;

    float height = heightMap[index];
    float terrainHeight = height * mapGenerationConfiguration.HeightMultiplier;
    float seaLevelHeight = erosionConfiguration.SeaLevel * mapGenerationConfiguration.HeightMultiplier;
    fragPosition = vec3(matModel * vec4(vertexPosition.xy, terrainHeight, 1.0));
    vec3 normal = getScaledNormal(x, y);
    fragNormal = transpose(inverse(mat3(matModel))) * normal;
    vec3 terrainColor = vec3(1.0);
    if(mapGenerationConfiguration.IsColorEnabled)
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
    fragColor = vec4(terrainColor, 1.0);
    fragPosLightSpace = lightSpaceMatrix * vec4(fragPosition, 1.0);
    gl_Position = mvp * vec4(vertexPosition.xy, terrainHeight, 1.0);
}