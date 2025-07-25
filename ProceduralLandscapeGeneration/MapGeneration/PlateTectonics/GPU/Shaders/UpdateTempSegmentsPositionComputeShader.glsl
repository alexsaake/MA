#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
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

struct PlateTectonicsSegment
{
    int Plate;
    float Mass;
    float Inertia;
    float Density;
    float Height;
    float Thickness;
    bool IsAlive;
    bool IsColliding;
    vec2 Position;
};

layout(std430, binding = 15) buffer plateTectonicsSegmentsShaderBuffer
{
    PlateTectonicsSegment[] plateTectonicsSegments;
};

struct PlateTectonicsPlate
{
    float Mass;
    float Inertia;
    float Rotation;
    float Torque;
    float AngularVelocity;
    int PlateSegments;
    vec2 Position;
    vec2 TempPosition;
    vec2 Acceleration;
    vec2 Speed;
};

layout(std430, binding = 16) readonly restrict buffer plateTectonicsPlatesShaderBuffer
{
    PlateTectonicsPlate[] plateTectonicsPlates;
};

layout(std430, binding = 17) buffer plateTectonicsTempSegmentsShaderBuffer
{
    PlateTectonicsSegment[] plateTectonicsTempSegments;
};

struct PlateTectonicsConfiguration
{
    float TransferRate;
    float SubductionHeating;
    float GenerationCooling;
    float GrowthRate;
    float DissolutionRate;
    float AccelerationConvection;
    float TorqueConvection;
    float DeltaTime;
};

layout(std430, binding = 19) buffer plateTectonicsConfigurationShaderBuffer
{
    PlateTectonicsConfiguration plateTectonicsConfiguration;
};

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
uint myHeightMapSideLength;
#define PI 3.1415926535897932384626433832795

uint GetIndexVector(ivec2 position)
{
    return (position.y * myHeightMapSideLength) + position.x;
}

float Angle(vec2 d)
{
    if (d.x == 0 && d.y == 0) return 0.0;
    if (d.x == 0 && d.y > 0) return PI / 2.0;
    if (d.x == 0 && d.y < 0) return 3.0 * PI / 2.0;

    float a = 2.0 * PI + atan(d.y / d.x);

    if (d.x < 0) a += PI;

    return a;
}

bool IsOutOfBounds(ivec2 position)
{
    return position.x < 0 || position.x >= myHeightMapSideLength
        || position.y < 0 || position.y >= myHeightMapSideLength;
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    uint heightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(id >= heightMapPlaneSize)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(heightMapPlaneSize));

    PlateTectonicsSegment plateTectonicsSegment = plateTectonicsSegments[id];
    PlateTectonicsPlate plateTectonicsPlate = plateTectonicsPlates[plateTectonicsSegment.Plate];
    
    vec2 direction = plateTectonicsSegment.Position - (plateTectonicsPlate.Position - plateTectonicsConfiguration.DeltaTime * plateTectonicsPlate.Speed);
    float angle = Angle(direction) - (plateTectonicsPlate.Rotation - plateTectonicsConfiguration.DeltaTime * plateTectonicsPlate.AngularVelocity);

    vec2 newPosition = plateTectonicsPlate.Position + length(direction) * vec2(cos(plateTectonicsPlate.Rotation + angle), sin(plateTectonicsPlate.Rotation + angle));
    if(IsOutOfBounds(ivec2(newPosition)))
    {
        return;
    }
    plateTectonicsSegment.Position = newPosition;

    plateTectonicsTempSegments[GetIndexVector(ivec2(plateTectonicsSegment.Position))] = plateTectonicsSegment;

    memoryBarrier();
}
