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
uint myHeightMapSideLength;
float Convection = 10.0f;

#define PI 3.1415926535897932384626433832795

vec2 Force(ivec2 position)
{
    float fx = 0.0;
    float fy = 0.0;

    if (position.x > 0 && position.x < myHeightMapSideLength - 2 && position.y > 0 && position.y < myHeightMapSideLength - 2)
    {
        fx = (heatMap[position.x + 1 + position.y * myHeightMapSideLength] - heatMap[position.x - 1 + position.y * myHeightMapSideLength]) / 2.0;
        fy = -(heatMap[position.x + (position.y + 1) * myHeightMapSideLength] - heatMap[position.x + (position.y - 1) * myHeightMapSideLength]) / 2.0;
    }

    //Out-of-Bounds
    if (position.x <= 0) fx = 0.0f;
    else if (position.x >= myHeightMapSideLength - 1) fx = -0.0f;
    if (position.y <= 0) fy = 0.0f;
    else if (position.y >= myHeightMapSideLength - 1) fy = -0.0f;

    return vec2(fx, fy);
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

void main()
{
    uint id = gl_GlobalInvocationID.x;
    uint plateTectonicsSegmentsLength = plateTectonicsSegments.length();
    if(id > plateTectonicsSegmentsLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(plateTectonicsSegmentsLength));

    PlateTectonicsSegment plateTectonicsSegment = plateTectonicsSegments[id];
    PlateTectonicsPlate plateTectonicsPlate = plateTectonicsPlates[plateTectonicsSegment.Plate];

    vec2 force = Force(ivec2(plateTectonicsSegment.Position));
    vec2 direction = plateTectonicsSegment.Position - plateTectonicsPlate.Position;

    plateTectonicsPlate.Acceleration -= Convection * force * 100;
    plateTectonicsPlate.Torque -= Convection * length(direction) * length(force) * sin(Angle(force) - Angle(direction));

    plateTectonicsPlates[plateTectonicsSegment.Plate] = plateTectonicsPlate;

    memoryBarrier();
}
