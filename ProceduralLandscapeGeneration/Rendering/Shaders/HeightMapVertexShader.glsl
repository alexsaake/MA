#version 430

layout(std430, binding = 1) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct Configuration
{
    float HeightMultiplier;
    float SeaLevel;
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
in vec4 vertexColor;

uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 lightSpaceMatrix;

out vec3 fragPosition;
out vec3 fragNormal;
out vec4 fragColor;
out vec4 fragPosLightSpace;

void main()
{
    uint index = gl_VertexID;
    myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint x = index % myHeightMapSideLength;
    uint y = index / myHeightMapSideLength;

    float height = heightMap[index] * configuration.HeightMultiplier;
    fragPosition = vec3(matModel * vec4(vertexPosition.xy, height, 1.0));
    fragNormal = transpose(inverse(mat3(matModel))) * getScaledNormal(x, y);
    fragColor = vertexColor;
    fragPosLightSpace = lightSpaceMatrix * vec4(fragPosition, 1.0);
    gl_Position = mvp * vec4(vertexPosition.xy, height, 1.0);
}