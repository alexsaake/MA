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

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    bool IsColorEnabled;
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
    
    uint sideLength = uint(sqrt(heightMap.length()));    
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