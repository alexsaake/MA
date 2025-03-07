﻿#version 430

layout (local_size_x = 16, local_size_y = 16, local_size_z = 1) in;

layout(std430, binding = 1) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct GridPoint
{
    float WaterHeight;
    float SuspendedSediment;
    float TempSediment;

    float FlowLeft;
    float FlowRight;
    float FlowTop;
    float FlowBottom;

    float VelocityX;
    float VelocityY;
};

layout(std430, binding = 2) buffer gridPointsShaderBuffer
{
    GridPoint[] gridPoints;
};

uint myHeightMapSideLength;

uint getIndex(vec2 position)
{
    return uint((position.y * myHeightMapSideLength) + position.x);
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute

float4 SampleBilinear(RWTexture2D<float4> tex, float2 uv)
{
	float2 uva = floor(uv);
	float2 uvb = ceil(uv);

	uint2 id00 = (uint2)uva;  // 0 0
	uint2 id10 = uint2(uvb.x, uva.y); // 1 0
	uint2 id01 = uint2(uva.x, uvb.y); // 0 1	
	uint2 id11 = (uint2)uvb; // 1 1

	float2 d = uv - uva;

	return
		tex[id00] * (1 - d.x) * (1 - d.y) +
		tex[id10] * d.x * (1 - d.y) +
		tex[id01] * (1 - d.x) * d.y +
		tex[id11] * d.x * d.y;
}

void main()
{    
    float dt = 0.25;

    uint id = gl_GlobalInvocationID.x;
    uint myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridPoint gridPoint = gridPoints[id];

	float4 state = CURRENT_SAMPLE(HeightMap);
	float2 velocity = CURRENT_SAMPLE(VelocityMap); 

	// Sediment advection
	SEDIMENT(state) = SEDIMENT(SampleBilinear(HeightMap, id.xy - velocity * _TimeDelta));

	// Write heightmap
	CURRENT_SAMPLE(HeightMap) = state;
}