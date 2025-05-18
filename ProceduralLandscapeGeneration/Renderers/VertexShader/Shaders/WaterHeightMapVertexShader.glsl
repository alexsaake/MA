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

void main()
{
    uint index = gl_VertexID;
    uint heightMapLength = heightMap.length() / mapGenerationConfiguration.LayerCount;
    if(index >= heightMapLength)
    {
        return;
    }
    uint sideLength = uint(sqrt(heightMapLength));    
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
    gl_Position =  mvp * vec4(vertexPosition.xy, (heightMap[index] - zOffset + waterHeight) * mapGenerationConfiguration.HeightMultiplier, 1.0);
}