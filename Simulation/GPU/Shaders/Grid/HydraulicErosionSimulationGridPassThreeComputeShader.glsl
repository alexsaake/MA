#version 430

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 1) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct GridPoint
{
    float WaterHeight;
    float SuspendedSediment;
    float TempSediment;

    float FlowLeft;
    float FlowRight;
    float FlowTop;
    float FlowBottom;

    float VelocityX;
    float VelocityY;
};

layout(std430, binding = 2) buffer gridPointsShaderBuffer
{
    GridPoint[] gridPoints;
};

uint myHeightMapSideLength;

uint getIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

void main()
{
    float Kc = 0.01;
    float Ks = 0.1;
    float Kd = 0.1;

    uint id = gl_GlobalInvocationID.x;
    myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;

    uint index = getIndex(x, y);

    vec3 normal = vec3(heightMap[getIndex(x + 1, y)] - heightMap[getIndex(x - 1, y)], heightMap[getIndex(x, y + 1)] - heightMap[getIndex(x, y - 1)], 2);
    normal = normalize(normal);
    float cosa = dot(normal, vec3(0, 0, 1));
    float sinAlpha = sin(acos(cosa));
    sinAlpha = max(sinAlpha, 0.1f);

    float capacity = Kc * sqrt(gridPoints[index].VelocityX * gridPoints[index].VelocityX + gridPoints[index].VelocityY * gridPoints[index].VelocityY) * sinAlpha;
    float delta = capacity - gridPoints[index].SuspendedSediment;

    if (delta > 0.0f)
    {
        float d = Ks * delta;
        heightMap[index] -= d;
        gridPoints[index].SuspendedSediment += d;
    }
    else if (delta < 0.0f)
    {
        float d = Kd * delta;
        heightMap[index] -= d;
        gridPoints[index].SuspendedSediment += d;
    }
}