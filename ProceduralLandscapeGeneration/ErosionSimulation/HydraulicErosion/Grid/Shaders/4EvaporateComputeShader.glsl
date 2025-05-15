#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct GridHydraulicErosionCell
{
    float WaterHeight;
    float SuspendedSediment;
    float TempSediment;
    float FlowLeft;
    float FlowRight;
    float FlowTop;
    float FlowBottom;
    vec2 Velocity;
};

layout(std430, binding = 4) buffer gridHydraulicErosionCellShaderBuffer
{
    GridHydraulicErosionCell[] gridHydraulicErosionCells;
};

struct GridErosionConfiguration
{
    float WaterIncrease;
    float Gravity;
    float Dampening;
    float MaximalErosionDepth;
    float SuspensionRate;
    float DepositionRate;
    float EvaporationRate;
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

layout(std430, binding = 9) buffer gridErosionConfigurationShaderBuffer
{
    GridErosionConfiguration gridErosionConfiguration;
};

uint myHeightMapSideLength;

uint getIndex(uint x, uint y)
{
    return uint((y * myHeightMapSideLength) + x);
}

uint getIndexVector(vec2 position)
{
    if(position.x < 0)
    {
        position.x = floor(position.x);
    }
    else if(position.x > 0)
    {
        position.x = ceil(position.x);
    }
    if(position.y < 0)
    {
        position.y = floor(position.y);
    }
    else if(position.y > 0)
    {
        position.y = ceil(position.y);
    }
    return uint((position.y * myHeightMapSideLength) + position.x);
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute

float SampleBilinearSediment(vec2 position)
{
	vec2 ceilPosition = ceil(position);
    if(ceilPosition.x >= myHeightMapSideLength)
    {
        ceilPosition.x = myHeightMapSideLength - 1;
    }
    if(ceilPosition.x < 0)
    {
        ceilPosition.x = 0;
    }
    if(ceilPosition.y >= myHeightMapSideLength)
    {
        ceilPosition.y = myHeightMapSideLength - 1;
    }
    if(ceilPosition.y < 0)
    {
        ceilPosition.y = 0;
    }
	vec2 floorPosition = floor(position);
    if(floorPosition.x >= myHeightMapSideLength)
    {
        floorPosition.x = myHeightMapSideLength - 1;
    }
    if(floorPosition.x < 0)
    {
        floorPosition.x = 0;
    }
    if(floorPosition.y >= myHeightMapSideLength)
    {
        floorPosition.y = myHeightMapSideLength - 1;
    }
    if(floorPosition.y < 0)
    {
        floorPosition.y = 0;
    }
    
    uint topLeftIndex = uint(ceilPosition.y * myHeightMapSideLength + floorPosition.x);
    uint topRightIndex = uint(ceilPosition.y * myHeightMapSideLength + ceilPosition.x);
    uint bottomLeftIndex = uint(floorPosition.y * myHeightMapSideLength + floorPosition.x);
    uint bottomRightIndex = uint(floorPosition.y * myHeightMapSideLength + ceilPosition.x);
    
    float dx = position.x - floorPosition.x;
    float dy = position.y - floorPosition.y;
    
    float interpolateXBottom = gridHydraulicErosionCells[bottomLeftIndex].SuspendedSediment * (1 - dx) + gridHydraulicErosionCells[bottomRightIndex].SuspendedSediment * dx;
    float interpolateXTop = gridHydraulicErosionCells[topLeftIndex].SuspendedSediment * (1 - dx) + gridHydraulicErosionCells[topRightIndex].SuspendedSediment * dx;
    
    // calculate interpolated sediment
    float interpolatedSediment = interpolateXBottom * (1 - dy) + interpolateXTop * dy;

    return interpolatedSediment;
}

void main()
{    
    uint id = gl_GlobalInvocationID.x;
    uint gridHydraulicErosionCellsLength = gridHydraulicErosionCells.length();
    if(id > gridHydraulicErosionCellsLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(gridHydraulicErosionCellsLength));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridHydraulicErosionCell gridHydraulicErosionCell  = gridHydraulicErosionCells[id];

    vec2 previousPosition = vec2(x, y) - gridHydraulicErosionCell.Velocity * erosionConfiguration.TimeDelta;
    if(previousPosition.x < 0 || previousPosition.x >= myHeightMapSideLength
        || previousPosition.y < 0 || previousPosition.y >= myHeightMapSideLength)
        {
            gridHydraulicErosionCell.TempSediment = SampleBilinearSediment(previousPosition);    
        }
    else
    {
        gridHydraulicErosionCell.TempSediment = gridHydraulicErosionCells[getIndexVector(previousPosition)].SuspendedSediment;
    }

	gridHydraulicErosionCell.WaterHeight = max(0.0, gridHydraulicErosionCell.WaterHeight * (1.0 - gridErosionConfiguration.EvaporationRate * erosionConfiguration.TimeDelta));

    gridHydraulicErosionCells[id] = gridHydraulicErosionCell;
    
    memoryBarrier();
}