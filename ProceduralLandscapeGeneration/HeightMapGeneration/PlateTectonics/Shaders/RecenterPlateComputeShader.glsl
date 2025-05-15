#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

struct PlateTectonicsSegment
{
    uint Plate;
    float Mass;
    float Inertia;
    vec2 Position;
};

layout(std430, binding = 15) readonly restrict buffer plateTectonicsSegmentsShaderBuffer
{
    PlateTectonicsSegment[] plateTectonicsSegments;
};

struct PlateTectonicsPlate
{
    float Mass;
    float Inertia;
    vec2 Position;
};

layout(std430, binding = 16) buffer plateTectonicsPlatesShaderBuffer
{
    PlateTectonicsPlate[] plateTectonicsPlates;
};

void main()
{
    uint id = gl_GlobalInvocationID.x;
    uint plateTectonicsPlatesLength = plateTectonicsPlates.length();
    if(id > plateTectonicsPlatesLength)
    {
        return;
    }

    PlateTectonicsPlate plateTectonicsPlate = plateTectonicsPlates[id];

    vec2 position = vec2(0.0);
    float mass = 0.0;
    float inertia = 0.0;
    
    uint plateSegments = 0;
    for(uint segment = 0; segment < plateTectonicsSegments.length(); segment++)
    {
        if(plateTectonicsSegments[segment].Plate == id)
        {
            position += plateTectonicsSegments[segment].Position;
            mass += plateTectonicsSegments[segment].Mass;
            inertia += pow((position - plateTectonicsSegments[segment].Position).length(), 2.0) * plateTectonicsSegments[segment].Mass;
            plateSegments++;
        }
    }

    position /= plateSegments;

    plateTectonicsPlate.Position = position;
    plateTectonicsPlate.Mass = mass;
    plateTectonicsPlate.Inertia = inertia;

    plateTectonicsPlates[id] = plateTectonicsPlate;

    memoryBarrier();
}
