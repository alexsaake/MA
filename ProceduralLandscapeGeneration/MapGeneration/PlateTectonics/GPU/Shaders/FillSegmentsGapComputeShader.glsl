#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

layout(std430, binding = 1) buffer heatMapShaderBuffer
{
    float[] heatMap;
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

uint GetIndexVector(ivec2 position)
{
    return (position.y * myHeightMapSideLength) + position.x;
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    uint heightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(id >= heightMapPlaneSize)
    {
        return;
    }

    PlateTectonicsSegment deadSegment = plateTectonicsSegments[id];
    if(deadSegment.IsAlive)
    {
        return;
    }
    uint heightMapSideLength = uint(sqrt(heightMapPlaneSize));

    uint x = id % heightMapSideLength;
    uint y = id / heightMapSideLength;

    deadSegment.Mass = 0.0;
    deadSegment.Inertia = 0.0;
    deadSegment.Density = 0.0;
    deadSegment.Height = 0.0;
    deadSegment.Thickness = 0.0;
    deadSegment.Position = vec2(x, y);
    deadSegment.IsAlive = true;
    deadSegment.IsColliding = false;
    
    float distance = float(heightMapPlaneSize);
    int nearestPlate = -1;
    for(int plate = 0; plate < plateTectonicsPlates.length(); plate++)
    {
        float plateToSegmentDistance = length(plateTectonicsPlates[plate].Position - deadSegment.Position);
        if(plateToSegmentDistance < distance)
        {
            distance = plateToSegmentDistance;
            nearestPlate = plate;
        }
    }
    deadSegment.Plate = nearestPlate;    

    plateTectonicsSegments[id] = deadSegment;
    
    for(int j = -2; j <= 2; j++)
    {
        for(int i = -2; i <= 2; i++)
        {
            uint generationCoolingIndex = GetIndexVector(ivec2(deadSegment.Position) + ivec2(i, j));
            heatMap[generationCoolingIndex] = clamp(heatMap[generationCoolingIndex] - plateTectonicsConfiguration.GenerationCooling, 0.0, 1.0);
        }
    }

    memoryBarrier();
}
