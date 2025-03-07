#version 430

layout (local_size_x = 16, local_size_y = 16, local_size_z = 1) in;

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

float GetFlowRight(uint x, uint y)
{
    if (x < 0 || x > myHeightMapSideLength - 1)
    {
        return 0.0;
    }
    return gridPoints[getIndex(x, y)].FlowRight;
}

float GetFlowLeft(uint x, uint y)
{
    if (x < 0 || x > myHeightMapSideLength - 1)
    {
        return 0.0;
    }
    return gridPoints[getIndex(x, y)].FlowLeft;
}

float GetFlowBottom(uint x, uint y)
{
    if (y < 0 || y > myHeightMapSideLength - 1)
    {
        return 0.0;
    }
    return gridPoints[getIndex(x, y)].FlowBottom;
}

float GetFlowTop(uint x, uint y)
{
    if (y < 0 || y > myHeightMapSideLength - 1)
    {
        return 0.0;
    }
    return gridPoints[getIndex(x, y)].FlowTop;
}

void main()
{
    float dt = 0.25;
    float dx = 1.0;
    float dy = 1.0;

    uint id = gl_GlobalInvocationID.x;
    myHeightMapSideLength = uint(sqrt(gridPoints.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridPoint gridPoint = gridPoints[id];

    float inFlow = GetFlowRight(x - 1, y) + GetFlowLeft(x + 1, y) + GetFlowTop(x, y - 1) + GetFlowBottom(x, y + 1);
    float outFlow = gridPoint.FlowRight + gridPoint.FlowLeft + gridPoint.FlowTop + gridPoint.FlowBottom;
    float dV = dt * (inFlow - outFlow);
    float oldWater = gridPoint.WaterHeight;
    gridPoint.WaterHeight += dV;
    gridPoint.WaterHeight = max(0.0, gridPoint.WaterHeight);

    float meanWater = 0.5 * (oldWater + gridPoint.WaterHeight);

    if (meanWater == 0.0)
    {
        gridPoint.VelocityX = 0.0;
        gridPoint.VelocityY = 0.0;
    }
    else
    {
        gridPoint.VelocityX = 0.5 * (GetFlowRight(x - 1, y) - GetFlowLeft(x, y) - GetFlowLeft(x + 1, y) + GetFlowRight(x, y)) / dy * meanWater;
        gridPoint.VelocityY = 0.5 * (GetFlowTop(x, y - 1) - GetFlowBottom(x, y) - GetFlowBottom(x, y + 1) + GetFlowTop(x, y)) / dx * meanWater;
    }
    
    //----------------------------------------------------

    float Kc = 10.0;
    float Ks = 1.0;
    float Kd = 1.0;

    vec3 normal = vec3(heightMap[getIndex(x + 1, y)] - heightMap[getIndex(x - 1, y)], heightMap[getIndex(x, y + 1)] - heightMap[getIndex(x, y - 1)], 2);
    normal = normalize(normal);
    float cosa = dot(normal, vec3(0, 0, 1));
    float sinAlpha = sin(acos(cosa));

    float capacity = Kc * sqrt(gridPoint.VelocityX * gridPoint.VelocityX + gridPoint.VelocityY * gridPoint.VelocityY) * sinAlpha;
    float delta = capacity - gridPoint.SuspendedSediment;

    if (delta > 0.0f)
    {
        float d = Ks * delta;
        heightMap[id] -= d;
        gridPoint.SuspendedSediment += d;
    }
    else if (delta < 0.0f)
    {
        float d = Kd * delta;
        heightMap[id] -= d;
        gridPoint.SuspendedSediment += d;
    }

    gridPoints[id] = gridPoint;
}