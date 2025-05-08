#version 430

layout(std430, binding = 1) readonly restrict buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    float SeaLevel;
    bool IsColorEnabled;
};

layout(std430, binding = 2) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
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

layout(std430, binding = 3) buffer gridPointsShaderBuffer
{
    GridPoint[] gridPoints;
};

struct ParticleHydraulicErosion
{
    int Age;
    float Volume;
    float Sediment;
    vec2 Position;
    vec2 Speed;
};

layout(std430, binding = 4) buffer particleHydraulicErosionShaderBuffer
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

layout(std430, binding = 5) buffer particleWindErosionShaderBuffer
{
    ParticleWindErosion[] particlesWindErosion;
};

in vec3 vertexPosition;

uniform mat4 mvp;

out vec4 fragColor;

vec4 sedimentColor = vec4(0.3, 0.2, 0.1, 0.5);

void main()
{
    uint index = gl_VertexID;
    
    uint sideLength = uint(sqrt(heightMap.length()));    
    uint x = index % sideLength;
    uint y = index / sideLength;

    float suspendedSediment = gridPoints[index].SuspendedSediment;
    for(int particle = 0; particle < particlesHydraulicErosion.length(); particle++)
    {        
        if(ivec2(particlesHydraulicErosion[particle].Position) == ivec2(x, y))
        {
            suspendedSediment = particlesHydraulicErosion[particle].Sediment;
            continue;
        }
    }
    for(int particle = 0; particle < particlesWindErosion.length(); particle++)
    {        
        if(ivec2(particlesWindErosion[particle].Position) == ivec2(x, y))
        {
            suspendedSediment = particlesWindErosion[particle].Sediment;
            continue;
        }
    }
    
    fragColor = sedimentColor;
    float zOffset = 0.00004;
    gl_Position =  mvp * vec4(vertexPosition.xy, (heightMap[index] - zOffset + suspendedSediment) * mapGenerationConfiguration.HeightMultiplier, 1.0);
}