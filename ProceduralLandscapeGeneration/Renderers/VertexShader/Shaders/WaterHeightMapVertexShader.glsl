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
    float FlowLeft;
    float FlowRight;
    float FlowUp;
    float FlowDown;
    float SuspendedSediment;
    float SedimentFlowLeft;
    float SedimentFlowRight;
    float SedimentFlowUp;
    float SedimentFlowDown;
    vec2 Velocity;
};

layout(std430, binding = 4) buffer gridHydraulicErosionCellShaderBuffer
{
    GridHydraulicErosionCell[] gridHydraulicErosionCells;
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

in vec3 vertexPosition;

uniform mat4 mvp;

out vec4 fragColor;

vec4 waterColor = vec4(0.0, 0.0, 1.0, 0.25);

uint myHeightMapLength;

float totalHeight(uint index)
{
    float height = 0;
    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        height += heightMap[index + layer * myHeightMapLength];
    }
    return height;
}

void main()
{
    uint index = gl_VertexID;
    myHeightMapLength = heightMap.length() / mapGenerationConfiguration.LayerCount;
    if(index >= myHeightMapLength)
    {
        return;
    }
    uint sideLength = uint(sqrt(myHeightMapLength));    
    uint x = index % sideLength;
    uint y = index / sideLength;

    float waterHeight = gridHydraulicErosionCells[index].WaterHeight;
    for(int particle = 0; particle < particlesHydraulicErosion.length(); particle++)
    {        
        if(ivec2(particlesHydraulicErosion[particle].Position) == ivec2(x, y))
        {
            waterHeight = particlesHydraulicErosion[particle].Volume;
            continue;
        }
    }

    fragColor = waterColor;
    float zOffset = 0.00004;
    float height = totalHeight(index);
    gl_Position =  mvp * vec4(vertexPosition.xy, (height - zOffset + waterHeight) * mapGenerationConfiguration.HeightMultiplier, 1.0);
}