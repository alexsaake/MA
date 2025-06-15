#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) readonly restrict buffer heightMapShaderBuffer
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

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
uint myHeightMapSideLength;

uint GetIndexVector(ivec2 position)
{
    return (position.y * myHeightMapSideLength) + position.x;
}

bool IsOutOfBounds(ivec2 position)
{
    return position.x < 0 || position.x >= myHeightMapSideLength
        || position.y < 0 || position.y >= myHeightMapSideLength;
}

PlateTectonicsSegment Buoyancy(PlateTectonicsSegment segment)
{
    segment.Density = segment.Mass / segment.Thickness;
    segment.Height = segment.Thickness * (1.0 - segment.Density);

    return segment;
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
    
    if(!plateTectonicsSegment.IsColliding)
    {
        return;
    }
    
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            ivec2 scanPosition = ivec2(plateTectonicsSegment.Position) + ivec2(x, y);
            if(IsOutOfBounds(scanPosition))
            {
                continue;
            }
            uint scanPositionIndex = GetIndexVector(scanPosition);
            PlateTectonicsSegment cascadingSegment = plateTectonicsSegments[scanPositionIndex];
            
            float heightDifference = plateTectonicsSegment.Height - cascadingSegment.Height;
            if(!cascadingSegment.IsAlive
                || heightDifference <= 0)
            {
                continue;
            }

            float massDifference = heightDifference * plateTectonicsSegment.Density;
            float transferRate = 0.2;

            float transferedHeight = transferRate * heightDifference;
            cascadingSegment.Thickness += transferedHeight;
            plateTectonicsSegment.Thickness -= transferedHeight;
            float transferedMass = transferRate * massDifference;
            cascadingSegment.Mass += transferedMass;
            plateTectonicsSegment.Mass -= transferedMass;

            cascadingSegment = Buoyancy(cascadingSegment);
            plateTectonicsSegment = Buoyancy(plateTectonicsSegment);

            plateTectonicsSegments[scanPositionIndex] = cascadingSegment;
        }
    }
    
    plateTectonicsSegment.IsColliding = false;

    plateTectonicsSegments[id] = plateTectonicsSegment;

    memoryBarrier();
}
