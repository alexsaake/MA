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

    vec2 Velocity;
};

layout(std430, binding = 2) buffer gridPointsShaderBuffer
{
    GridPoint[] gridPoints;
};

struct GridErosionConfiguration
{
    float TimeDelta;
    float CellSizeX;
    float CellSizeY;
    float Gravity;
    float Friction;
    float MaximalErosionDepth;
    float SedimentCapacity;
    float SuspensionRate;
    float DepositionRate;
    float SedimentSofteningRate;
    float EvaporationRate;
};

layout(std430, binding = 3) buffer gridErosionConfigurationShaderBuffer
{
    GridErosionConfiguration gridErosionConfiguration;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    float SeaLevel;
    bool IsColorEnabled;
};

layout(std430, binding = 4) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

uint myHeightMapSideLength;

uint getIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute
//https://lisyarus.github.io/blog/posts/simulating-water-over-terrain.html
//https://github.com/karhu/terrain-erosion/blob/master/Simulation/FluidSimulation.cpp

void main()
{    
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
    
    float height = heightMap[id];
    if(height <= mapGenerationConfiguration.SeaLevel)
    {
        gridPoint.WaterHeight = mapGenerationConfiguration.SeaLevel - height;
    }

    float totalHeight = height + gridPoint.WaterHeight;
    float frictionFactor = pow(1 - gridErosionConfiguration.Friction, gridErosionConfiguration.TimeDelta);

    if(x > 0)
    {
        float totalHeightLeft = heightMap[getIndex(x - 1, y)] + gridPoints[getIndex(x - 1, y)].WaterHeight;
        gridPoint.FlowLeft = max(0.0, gridPoint.FlowLeft * frictionFactor + (totalHeight - totalHeightLeft) * gridErosionConfiguration.Gravity * gridErosionConfiguration.TimeDelta / gridErosionConfiguration.CellSizeX);
    }
    else
    {
        gridPoint.FlowLeft = 0.0;
    }

    if(x < myHeightMapSideLength - 1)
    {
        float totalHeightRight = heightMap[getIndex(x + 1, y)] + gridPoints[getIndex(x + 1, y)].WaterHeight;
        gridPoint.FlowRight = max(0.0, gridPoint.FlowRight * frictionFactor + (totalHeight - totalHeightRight) * gridErosionConfiguration.Gravity * gridErosionConfiguration.TimeDelta / gridErosionConfiguration.CellSizeX);
    }
    else
    {
        gridPoint.FlowRight = 0.0;
    }

    if(y > 0)
    {
        float totalHeightBottom = heightMap[getIndex(x, y - 1)] + gridPoints[getIndex(x, y - 1)].WaterHeight;
        gridPoint.FlowBottom = max(0.0, gridPoint.FlowBottom * frictionFactor + (totalHeight - totalHeightBottom) * gridErosionConfiguration.Gravity * gridErosionConfiguration.TimeDelta / gridErosionConfiguration.CellSizeY);
    }
    else
    {
        gridPoint.FlowBottom = 0.0;
    }

    if(y < myHeightMapSideLength - 1)
    {
        float totalHeightTop = heightMap[getIndex(x, y + 1)] + gridPoints[getIndex(x, y + 1)].WaterHeight;
        gridPoint.FlowTop = max(0.0, gridPoint.FlowTop * frictionFactor + (totalHeight - totalHeightTop) * gridErosionConfiguration.Gravity * gridErosionConfiguration.TimeDelta / gridErosionConfiguration.CellSizeY);
    }
    else
    {
        gridPoint.FlowTop = 0.0;
    }

    float totalOutflow = gridPoint.FlowLeft + gridPoint.FlowRight + gridPoint.FlowBottom + gridPoint.FlowTop;
    if (totalOutflow > gridPoint.WaterHeight)
    {
        float scale = min(1.0, gridPoint.WaterHeight * gridErosionConfiguration.CellSizeX * gridErosionConfiguration.CellSizeY / (totalOutflow * gridErosionConfiguration.TimeDelta));
        
        gridPoint.FlowLeft *= scale;
        gridPoint.FlowRight *= scale;
        gridPoint.FlowBottom *= scale;
        gridPoint.FlowTop *= scale;
    }

    gridPoints[id] = gridPoint;
    
    memoryBarrier();
}