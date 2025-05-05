#version 430

layout(std430, binding = 1) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct Configuration
{
    float HeightMultiplier;
    float SeaLevel;
    float IsColorEnabled;
};

layout(std430, binding = 2) readonly restrict buffer configurationShaderBuffer
{
    Configuration configuration;
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

    float xp1ym1 = heightMap[getIndex(x + 1, y - 1)];
    float xm1ym1 = heightMap[getIndex(x - 1, y - 1)];
    float xp1y = heightMap[getIndex(x + 1, y)];
    float xm1y = heightMap[getIndex(x - 1, y)];
    float xp1yp1 = heightMap[getIndex(x + 1, y + 1)];
    float xm1yp1 = heightMap[getIndex(x - 1, y + 1)];
    float xyp1 = heightMap[getIndex(x, y + 1)];
    float xym1 = heightMap[getIndex(x, y - 1)];

    vec3 normal = vec3(
    configuration.HeightMultiplier * -(xp1ym1 - xm1ym1 + 2 * (xp1y - xm1y) + xp1yp1 - xm1yp1),
    configuration.HeightMultiplier * -(xm1yp1 - xm1ym1 + 2 * (xyp1 - xym1) + xp1yp1 - xp1ym1),
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
    float terrainHeight = height * configuration.HeightMultiplier;
    float waterHeight = configuration.SeaLevel * configuration.HeightMultiplier;
    fragPosition = vec3(matModel * vec4(vertexPosition.xy, terrainHeight, 1.0));
    vec3 normal = getScaledNormal(x, y);
    fragNormal = transpose(inverse(mat3(matModel))) * normal;
    vec3 terrainColor = vec3(1.0);
    if(configuration.IsColorEnabled == 1)
    {
        if(terrainHeight < waterHeight + 0.3)
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