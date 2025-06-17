#version 430

layout(std430, binding = 0) readonly restrict buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct ParticleHydraulicErosion
{
    int Age;
    float Volume;
    float Sediment;
    vec2 Position;
    vec2 Speed;
};

layout(std430, binding = 2) buffer particleHydraulicErosionShaderBuffer
{
    ParticleHydraulicErosion[] particlesHydraulicErosion;
};

struct ParticleWindErosion
{
    int Age;
    float Sediment;
    vec3 Position;
    vec3 Speed;
};

layout(std430, binding = 3) buffer particleWindErosionShaderBuffer
{
    ParticleWindErosion[] particlesWindErosion;
};

struct GridHydraulicErosionCell
{
    float WaterHeight;

    float WaterFlowLeft;
    float WaterFlowRight;
    float WaterFlowUp;
    float WaterFlowDown;

    float SuspendedSediment;

    float SedimentFlowLeft;
    float SedimentFlowRight;
    float SedimentFlowUp;
    float SedimentFlowDown;

    vec2 WaterVelocity;
};

layout(std430, binding = 4) buffer gridHydraulicErosionCellShaderBuffer
{
    GridHydraulicErosionCell[] gridHydraulicErosionCells;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    uint RockTypeCount;
    uint LayerCount;
    float SeaLevel;
    bool AreTerrainColorsEnabled;
    bool ArePlateTectonicsPlateColorsEnabled;
    bool AreLayerColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};


uint myHeightMapPlaneSize;

uint LayerHydraulicErosionCellsOffset(uint layer)
{
    return layer * myHeightMapPlaneSize;
}

float TotalSuspendedSediment(uint index)
{    
    float suspendedSediment = 0.0;
    for(int layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        suspendedSediment += gridHydraulicErosionCells[index + LayerHydraulicErosionCellsOffset(layer)].SuspendedSediment;
    }
    return suspendedSediment;
}

float HeightMapLayerFloorHeight(uint index, uint layer)
{
    if(layer < 1
        || layer >= mapGenerationConfiguration.LayerCount)
    {
        return 0.0;
    }
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

uint HeightMapLayerOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

uint HeightMapRockTypeOffset(uint rockType)
{
    return rockType * myHeightMapPlaneSize;
}

float HeightMapLayerHeight(uint index, uint layer)
{
    float heightMapLayerHeight = 0.0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        heightMapLayerHeight += heightMap[index + HeightMapRockTypeOffset(rockType) + HeightMapLayerOffset(layer)];
    }
    return heightMapLayerHeight;
}

float TotalHeightMapHeight(uint index)
{
    float heightMapLayerFloorHeight = 0.0;
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        heightMapLayerFloorHeight = 0.0;
        if(layer > 0)
        {
            heightMapLayerFloorHeight = HeightMapLayerFloorHeight(index, layer);
            if(heightMapLayerFloorHeight == 0)
            {
                return 0.0;
            }
        }
        float heightMapLayerHeight = HeightMapLayerHeight(index, layer);
        if(heightMapLayerHeight > 0)
        {
            return heightMapLayerFloorHeight + heightMapLayerHeight;
        }
    }
    return 0.0;
}

in vec3 vertexPosition;

uniform mat4 mvp;

out vec4 fragColor;

vec4 sedimentColor = vec4(0.3, 0.2, 0.1, 0.5);

void main()
{
    uint index = gl_VertexID;
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= myHeightMapPlaneSize)
    {
        return;
    }
    uint sideLength = uint(sqrt(myHeightMapPlaneSize));    
    uint x = index % sideLength;
    uint y = index / sideLength;

    float totalSuspendedSediment = TotalSuspendedSediment(index);
    for(int particle = 0; particle < particlesHydraulicErosion.length(); particle++)
    {        
        if(ivec2(particlesHydraulicErosion[particle].Position) == ivec2(x, y))
        {
            totalSuspendedSediment += particlesHydraulicErosion[particle].Sediment;
            continue;
        }
    }
    for(int particle = 0; particle < particlesWindErosion.length(); particle++)
    {        
        if(ivec2(particlesWindErosion[particle].Position) == ivec2(x, y))
        {
            totalSuspendedSediment += particlesWindErosion[particle].Sediment;
            continue;
        }
    }
    
    fragColor = sedimentColor;
    float zOffset = 0.00004;
    float height = TotalHeightMapHeight(index);
    gl_Position =  mvp * vec4(vertexPosition.xy, (height - zOffset + totalSuspendedSediment) * mapGenerationConfiguration.HeightMultiplier, 1.0);
}