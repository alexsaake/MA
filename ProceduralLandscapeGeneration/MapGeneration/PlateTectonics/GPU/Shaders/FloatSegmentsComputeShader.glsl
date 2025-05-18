#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

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

layout(std430, binding = 15) buffer plateTectonicsSegmentsShaderBuffer
{
    PlateTectonicsSegment[] plateTectonicsSegments;
};

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
float Growth = 0.05f;

float Langmuir(float k, float x)
{
    return k * x / (1.0 + k * x);
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
    uint plateTectonicsSegmentsLength = plateTectonicsSegments.length();
    if(id >= plateTectonicsSegmentsLength)
    {
        return;
    }

    PlateTectonicsSegment plateTectonicsSegment = plateTectonicsSegments[id];

    if (!plateTectonicsSegment.IsAlive)
    {
        return;
    }

    float heatValue = heatMap[id];

    float rate = Growth * (1.0 - heatValue);
    float G = rate * (1.0 - heatValue - plateTectonicsSegment.Density * plateTectonicsSegment.Thickness);
    if (G < 0.0) G *= 0.05;

    float D = Langmuir(3.0, 1.0 - heatValue);

    plateTectonicsSegment.Mass += G * D;
    plateTectonicsSegment.Thickness += G;

    plateTectonicsSegment = Buoyancy(plateTectonicsSegment);

    plateTectonicsSegments[id] = plateTectonicsSegment;

    memoryBarrier();
}
