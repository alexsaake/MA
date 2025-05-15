#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

struct PlateTectonicsSegment
{
    uint Plate;
    float Mass;
    float Inertia;
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
    vec2 Position;
};

layout(std430, binding = 16) readonly restrict buffer plateTectonicsPlatesShaderBuffer
{
    PlateTectonicsPlate[] plateTectonicsPlates;
};

void main()
{
    uint id = gl_GlobalInvocationID.x;
    uint plateTectonicsSegmentsLength = plateTectonicsSegments.length();
    if(id > plateTectonicsSegmentsLength)
    {
        return;
    }
    uint heightMapSideLength = uint(sqrt(plateTectonicsSegmentsLength));

    uint x = id % heightMapSideLength;
    uint y = id / heightMapSideLength;

    PlateTectonicsSegment plateTectonicsSegment = plateTectonicsSegments[id];
    plateTectonicsSegment.Position = vec2(x, y);
    
    float distance = float(plateTectonicsSegmentsLength);
    uint nearestPlate = 0;
    for(uint plate = 0; plate < plateTectonicsPlates.length(); plate++)
    {
        float plateToSegmentDistance = length(plateTectonicsPlates[plate].Position - plateTectonicsSegment.Position);
        if(plateToSegmentDistance < distance)
        {
            distance = plateToSegmentDistance;
            nearestPlate = plate;
        }
    }
    plateTectonicsSegment.Plate = nearestPlate;

    plateTectonicsSegments[id] = plateTectonicsSegment;

    memoryBarrier();
}
