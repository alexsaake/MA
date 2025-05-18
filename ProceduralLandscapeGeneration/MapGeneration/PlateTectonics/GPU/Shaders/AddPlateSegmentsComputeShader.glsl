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

layout(std430, binding = 15) readonly restrict buffer plateTectonicsSegmentsShaderBuffer
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

layout(std430, binding = 16) buffer plateTectonicsPlatesShaderBuffer
{
    PlateTectonicsPlate[] plateTectonicsPlates;
};

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
void main()
{
    uint id = gl_GlobalInvocationID.x;
    if(id >= plateTectonicsSegments.length())
    {
        return;
    }

    PlateTectonicsSegment plateTectonicsSegment = plateTectonicsSegments[id];
    PlateTectonicsPlate plateTectonicsPlate = plateTectonicsPlates[plateTectonicsSegment.Plate];

    plateTectonicsPlate.PlateSegments++;
    plateTectonicsPlate.TempPosition += plateTectonicsSegment.Position;
    plateTectonicsPlate.Mass += plateTectonicsSegment.Mass;
    plateTectonicsPlate.Inertia += pow(length(plateTectonicsPlate.Position - plateTectonicsSegment.Position), 2.0) * plateTectonicsSegment.Mass;

    plateTectonicsPlates[plateTectonicsSegment.Plate] = plateTectonicsPlate;

    memoryBarrier();
}
