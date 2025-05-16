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
//https://github.com/weigert/SimpleTectonics/blob/master/source/tectonics.h
uint myHeightMapSideLength;
float subductionHeating = 0.1;

PlateTectonicsSegment Buoyancy(PlateTectonicsSegment segment)
{
    segment.Density = segment.Mass / segment.Thickness;
    segment.Height = segment.Thickness * (1.0 - segment.Density);

    return segment;
}

uint getIndexV(ivec2 position)
{
    return (position.y * myHeightMapSideLength) + position.x;
}

bool IsOutOfBounds(ivec2 position)
{
    return position.x < 0 || position.x >= myHeightMapSideLength
        || position.y < 0 || position.y >= myHeightMapSideLength;
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    uint plateTectonicsSegmentsLength = plateTectonicsSegments.length();
    if(id >= plateTectonicsSegmentsLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(plateTectonicsSegmentsLength));

    PlateTectonicsSegment plateTectonicsSegment = plateTectonicsSegments[id];

    if(IsOutOfBounds(ivec2(plateTectonicsSegment.Position)))
    {
        plateTectonicsSegment.IsAlive = false;
        plateTectonicsSegment.Plate = -1;
        return;
    }

    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            ivec2 scanPosition = ivec2(plateTectonicsSegment.Position) + ivec2(x, y);
            if(IsOutOfBounds(scanPosition))
            {
                continue;
            }

            uint scanPositionIndex = getIndexV(scanPosition);
            PlateTectonicsSegment collidingSegment = plateTectonicsSegments[scanPositionIndex];
            if(ivec2(plateTectonicsSegment.Position) != ivec2(collidingSegment.Position)
                || !collidingSegment.IsAlive
                || plateTectonicsSegment.Plate == collidingSegment.Plate)
            {
                continue;
            }

            //Two Segments are Colliding, Subduce the Denser One
            if(plateTectonicsSegment.Density > collidingSegment.Density
                && !collidingSegment.IsColliding)
            {
                collidingSegment.IsColliding = true;
                plateTectonicsSegment.IsAlive = false;
                plateTectonicsSegment.Plate = -1;

                float massDifference = plateTectonicsSegment.Height * plateTectonicsSegment.Density;
                float heightDifference = plateTectonicsSegment.Height;

                collidingSegment.Thickness += heightDifference;  //Move Mass
                collidingSegment.Mass += massDifference;
                collidingSegment = Buoyancy(collidingSegment);

                plateTectonicsSegments[scanPositionIndex] = collidingSegment;

                heatMap[scanPositionIndex] += subductionHeating;

                break;
            }
        }
    }

    plateTectonicsSegments[id] = plateTectonicsSegment;

    memoryBarrier();
}
