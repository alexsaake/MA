#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

struct PlateTectonicsPlate
{
    float Mass;
    float Inertia;
    float Rotation;
    float Torque;
    float AngularVelocity;
    vec2 Position;
    vec2 Acceleration;
    vec2 Speed;
};

layout(std430, binding = 16) buffer plateTectonicsPlatesShaderBuffer
{
    PlateTectonicsPlate[] plateTectonicsPlates;
};

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
#define PI 3.1415926535897932384626433832795
float DT = 0.025;

void main()
{
    uint id = gl_GlobalInvocationID.x;
    if(id > plateTectonicsPlates.length())
    {
        return;
    }

    PlateTectonicsPlate plateTectonicsPlate = plateTectonicsPlates[id];
    
    plateTectonicsPlate.Speed += DT * plateTectonicsPlate.Acceleration / plateTectonicsPlate.Mass;
    if (plateTectonicsPlate.Inertia == 0)
    {
        return;
    }
    plateTectonicsPlate.AngularVelocity += DT * plateTectonicsPlate.Torque / plateTectonicsPlate.Inertia;
    plateTectonicsPlate.Position += DT * plateTectonicsPlate.Speed;
    plateTectonicsPlate.Rotation += DT * plateTectonicsPlate.AngularVelocity;

    if (plateTectonicsPlate.Rotation > 2 * PI) plateTectonicsPlate.Rotation -= 2 * PI;
    if (plateTectonicsPlate.Rotation < 0) plateTectonicsPlate.Rotation += 2 * PI;

    plateTectonicsPlates[id] = plateTectonicsPlate;

    memoryBarrier();
}
