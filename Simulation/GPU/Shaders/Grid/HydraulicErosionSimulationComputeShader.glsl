#version 430

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 1) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

layout(std430, binding = 3) readonly restrict buffer erosionConfigurationShaderBuffer
{
    uint heightMultiplier;
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

layout(std430, binding = 4) buffer gridPointsShaderBuffer
{
    GridPoint[] gridPoints;
};

uint myHeightMapSideLength;

uint getIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

uint getIndexV(ivec2 position)
{
    return (position.y * myHeightMapSideLength) + position.x;
}

bool isOutOfBounds(ivec2 position)
{
    return position.x < 0 || position.x > myHeightMapSideLength || position.y < 0 || position.y > myHeightMapSideLength;
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
    heightMultiplier * -(xp1ym1 - xm1ym1 + 2 * (xp1y - xm1y) + xp1yp1 - xm1yp1),
    heightMultiplier * -(xm1yp1 - xm1ym1 + 2 * (xyp1 - xym1) + xp1yp1 - xp1ym1),
    1.0);

    return normalize(normal);
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;

    
    float A = 0.00005f;
    float g = 9.81f;
    float l = 1.0f;

    float fluxFactor = A * g / l;

    float dh;
    float h0 = heightMap[x, y] + gridPoints[x, y].WaterHeight;
    float newFlux;

    if(x > 0)
    {
        dh = h0 - heightMap.Height[x - 1, y] + gridPoints[x - 1, y].WaterHeight;
        newFlux = gridPoints[x, y].FlowLeft + fluxFactor * dh;
        gridPoints[x, y].FlowLeft = max(0.0f, newFlux);
    }
    else
    {
        gridPoints[x, y].FlowLeft = 0.0;
    }

    dh = h0 - heightMap.Height[x + 1, y] + gridPoints[x + 1, y].WaterHeight;
    newFlux = gridPoints[x, y].FlowRight + fluxFactor * dh;
    gridPoints[x, y].FlowRight = Max(0.0f, newFlux);

    dh = h0 - heightMap.Height[x, y - 1] + gridPoints[x, y - 1].WaterHeight;
    newFlux = gridPoints[x, y].FlowBottom + fluxFactor * dh;
    gridPoints[x, y].FlowBottom = Max(0.0f, newFlux);

    dh = h0 - heightMap.Height[x, y + 1] + gridPoints[x, y + 1].WaterHeight;
    newFlux = gridPoints[x, y].FlowTop + fluxFactor * dh;
    gridPoints[x, y].FlowTop = Max(0.0f, newFlux);

    float sumFlux = gridPoints[x, y].FlowLeft + gridPoints[x, y].FlowRight + gridPoints[x, y].FlowBottom + gridPoints[x, y].FlowTop;
    if (sumFlux > 0.0f)
    {
        float K = Min(1.0f, gridPoints[x, y].WaterHeight / sumFlux);

        gridPoints[x, y].FlowLeft *= K;
        gridPoints[x, y].FlowRight *= K;
        gridPoints[x, y].FlowBottom *= K;
        gridPoints[x, y].FlowTop *= K;
    }
}