#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 1) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct GridPoint
{
    float WaterHeight;
    float SuspendedSediment;
    float TempSediment;
    float Hardness;

    float FlowLeft;
    float FlowRight;
    float FlowTop;
    float FlowBottom;

    float ThermalLeft;
    float ThermalRight;
    float ThermalTop;
    float ThermalBottom;

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
    return uint((y * myHeightMapSideLength) + x);
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute

void main()
{    
    float timeDelta = 1.0;
    float thermalErosionTimeScale = 1.0;
    
    uint id = gl_GlobalInvocationID.x;
    uint heightMapLength = heightMap.length();
    if(id > heightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(gridPoints.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridPoint gridPoint = gridPoints[id];

    float thermalIn = gridPoints[getIndex(x - 1, y)].ThermalRight + gridPoints[getIndex(x + 1, y)].ThermalLeft + gridPoints[getIndex(x, y - 1)].ThermalTop + gridPoints[getIndex(x, y + 1)].ThermalBottom;
    float thermalOut = gridPoint.ThermalRight + gridPoint.ThermalLeft + gridPoint.ThermalTop + gridPoint.ThermalBottom;

	float volumeDelta = thermalIn - thermalOut;

	heightMap[id] += min(1.0, timeDelta * thermalErosionTimeScale) * volumeDelta;
}