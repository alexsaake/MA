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
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

in vec3 vertexPosition;

uniform mat4 mvp;

out vec4 fragColor;

vec4 waterColor = vec4(0.0, 0.0, 1.0, 0.25);

uint myHeightMapPlaneSize;

float TotalHeightAllLayers(uint index)
{
    float height = 0;
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        if(layer > 0)
        {
            height += heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
        }
        for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
        {
            height += heightMap[index + rockType * myHeightMapPlaneSize + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize];
        }
        if(height > 0)
        {
            return height;
        }
    }
    return height;
}

float TotalWaterHeight(uint index)
{
    float waterHeight = 0;
    for(int layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        waterHeight += gridHydraulicErosionCells[index + layer * myHeightMapPlaneSize].WaterHeight;
    }
    return waterHeight;
}

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

    float waterHeight = TotalWaterHeight(index);
    for(int particle = 0; particle < particlesHydraulicErosion.length(); particle++)
    {        
        if(ivec2(particlesHydraulicErosion[particle].Position) == ivec2(x, y))
        {
            waterHeight += particlesHydraulicErosion[particle].Volume;
            continue;
        }
    }

    fragColor = waterColor;
    float zOffset = 0.00004;
    float height = TotalHeightAllLayers(index);
    gl_Position =  mvp * vec4(vertexPosition.xy, (height - zOffset + waterHeight) * mapGenerationConfiguration.HeightMultiplier, 1.0);
}