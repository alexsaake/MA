#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

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

layout(std430, binding = 17) buffer plateTectonicsTempSegmentsShaderBuffer
{
    PlateTectonicsSegment[] plateTectonicsTempSegments;
};

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/

void main()
{
    uint id = gl_GlobalInvocationID.x;
    uint plateTectonicsSegmentsLength = plateTectonicsSegments.length();
    if(id > plateTectonicsSegmentsLength)
    {
        return;
    }

    PlateTectonicsSegment plateTectonicsTempSegment = plateTectonicsTempSegments[id];

    if(plateTectonicsTempSegment.IsAlive)
    {
        plateTectonicsSegments[id] = plateTectonicsTempSegment;
    }

    plateTectonicsTempSegment.IsAlive = false;
    
    plateTectonicsTempSegments[id] = plateTectonicsTempSegment;

    memoryBarrier();
}
