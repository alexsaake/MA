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

layout(std430, binding = 16) readonly restrict buffer plateTectonicsPlatesShaderBuffer
{
    PlateTectonicsPlate[] plateTectonicsPlates;
};

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
uint myHeightMapSideLength;
float generationCooling = -0.1;

void main()
{
    uint id = gl_GlobalInvocationID.x;
    uint plateTectonicsSegmentsLength = plateTectonicsSegments.length();
    if(id > plateTectonicsSegmentsLength)
    {
        return;
    }

    PlateTectonicsSegment deadSegment = plateTectonicsSegments[id];
    if(deadSegment.IsAlive)
    {
        return;
    }
    uint heightMapSideLength = uint(sqrt(plateTectonicsSegmentsLength));

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
    
    float distance = float(plateTectonicsSegmentsLength);
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
    
    heatMap[id] += generationCooling;

    memoryBarrier();
}
