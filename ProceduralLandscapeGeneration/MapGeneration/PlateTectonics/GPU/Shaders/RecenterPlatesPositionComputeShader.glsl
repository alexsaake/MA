#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

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
    if(id >= plateTectonicsPlates.length())
    {
        return;
    }

    PlateTectonicsPlate plateTectonicsPlate = plateTectonicsPlates[id];

    if(plateTectonicsPlate.PlateSegments > 0)
    {
        plateTectonicsPlate.Position = plateTectonicsPlate.TempPosition / plateTectonicsPlate.PlateSegments;
    }
    else
    {
        plateTectonicsPlate.Position = vec2(0.0);
    }

    plateTectonicsPlates[id] = plateTectonicsPlate;

    memoryBarrier();
}
