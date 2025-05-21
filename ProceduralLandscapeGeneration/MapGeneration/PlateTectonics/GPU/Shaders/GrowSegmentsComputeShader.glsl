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

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    uint RockTypeCount;
    uint LayerCount;
    bool AreTerrainColorsEnabled;
    bool ArePlateTectonicsPlateColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
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

const float Growth = 0.05;

float Langmuir(float k, float x)
{
    return k * x / (1.0 + k * x);
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    uint heightMapLength = heightMap.length() / mapGenerationConfiguration.RockTypeCount / mapGenerationConfiguration.LayerCount;
    if(id >= heightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(heightMapLength));

    PlateTectonicsSegment plateTectonicsSegment = plateTectonicsSegments[id];

    if(!plateTectonicsSegment.IsAlive)
    {
        return;
    }

    float nd = heatMap[id];

    //LINEAR GROWTH RATE [m / s]
    float G = Growth * (1.0 - nd) * (1.0 - nd - plateTectonicsSegment.Density * plateTectonicsSegment.Thickness);
    if (G < 0.0) G *= 0.05;  //Dissolution Rate

    //COMPUTE EQUILIBRIUM DENSITY (PER-VOLUME)
    float D = Langmuir(3.0, 1.0 - nd);

    plateTectonicsSegment.Mass += G * D; //m^2 * m / s * kg / m^3 = kg
    plateTectonicsSegment.Thickness += G; //New Thickness

    plateTectonicsSegment.Density = plateTectonicsSegment.Mass / plateTectonicsSegment.Thickness;
    plateTectonicsSegment.Height = plateTectonicsSegment.Thickness * (1.0 - plateTectonicsSegment.Density);

    plateTectonicsSegments[id] = plateTectonicsSegment;

    memoryBarrier();
}
