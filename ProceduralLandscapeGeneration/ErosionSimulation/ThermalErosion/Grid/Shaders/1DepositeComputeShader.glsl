#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

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

struct GridThermalErosionCell
{
    float FlowLeft;
    float FlowRight;
    float FlowUp;
    float FlowDown;
};

layout(std430, binding = 13) buffer gridThermalErosionCellShaderBuffer
{
    GridThermalErosionCell[] gridThermalErosionCells;
};

struct LayersConfiguration
{
    float Hardness;
    float TangensTalusAngle;
};

layout(std430, binding = 18) buffer layersConfigurationShaderBuffer
{
    LayersConfiguration[] layersConfiguration;
};

uint myHeightMapSideLength;
uint myHeightMapLength;

void RemoveFromTop(uint index, float sediment)
{
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        uint offsetIndex = index + layer * myHeightMapLength;
        float height = heightMap[offsetIndex];
        if(height >= sediment)
        {
            heightMap[offsetIndex] -= height;
            break;
        }
        else
        {
            heightMap[offsetIndex] = 0;
            sediment -= height;
        }
    }
}

void DepositeOnTop(uint index, float sediment)
{
    heightMap[index + (mapGenerationConfiguration.LayerCount - 1) * myHeightMapLength] += sediment;
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
    return uint((y * myHeightMapSideLength) + x);
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute

void main()
{    
    uint id = gl_GlobalInvocationID.x;
    myHeightMapLength = heightMap.length() / mapGenerationConfiguration.LayerCount;
    if(id >= myHeightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapLength));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridThermalErosionCell gridThermalErosionCell = gridThermalErosionCells[id];

    float flowIn = gridThermalErosionCells[getIndex(x - 1, y)].FlowRight + gridThermalErosionCells[getIndex(x + 1, y)].FlowLeft + gridThermalErosionCells[getIndex(x, y - 1)].FlowUp + gridThermalErosionCells[getIndex(x, y + 1)].FlowDown;
    float flowOut = gridThermalErosionCell.FlowRight + gridThermalErosionCell.FlowLeft + gridThermalErosionCell.FlowUp + gridThermalErosionCell.FlowDown;

	float volumeDelta = (flowIn - flowOut) * erosionConfiguration.TimeDelta;
    if(volumeDelta < 0)
    {
        RemoveFromTop(id, abs(volumeDelta));
    }
    else
    {
        DepositeOnTop(id, volumeDelta);
    }
    
    memoryBarrier();
}