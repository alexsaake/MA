#version 430

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

vec2 SampleBilinear(vec2 uv)
{
	vec2 uva = floor(uv);
	vec2 uvb = ceil(uv);

	uvec2 id00 = uvec2(uva);  // 0 0
	uvec2 id10 = uvec2(uvb.x, uva.y); // 1 0
	uvec2 id01 = uvec2(uva.x, uvb.y); // 0 1	
	uvec2 id11 = uvec2(uvb); // 1 1

	float2 d = uv - uva;

	return
		heightMap[getIndex(id00)] * (1 - d.x) * (1 - d.y) +
		heightMap[getIndex(id10)] * d.x * (1 - d.y) +
		heightMap[getIndex(id01)] * (1 - d.x) * d.y +
		heightMap[getIndex(id11)] * d.x * d.y;
}

void main()
{    
    float dt = 0.25;

    uint id = gl_GlobalInvocationID.x;
    uint myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridPoint gridPoint = gridPoints[id];

	// Sample the heighmap (state map)
	float4 state = CURRENT_SAMPLE(HeightMap);
	float4 stateLeft = LEFT_SAMPLE(HeightMap);
	float4 stateRight = RIGHT_SAMPLE(HeightMap);
	float4 stateTop = TOP_SAMPLE(HeightMap);
	float4 stateBottom = BOTTOM_SAMPLE(HeightMap);
	float2 velocity = CURRENT_SAMPLE(VelocityMap);


	// Tilt angle computation
	float3 dhdx = float3(2 * _CellSize.x, TERRAIN_HEIGHT(stateRight) - TERRAIN_HEIGHT(stateLeft), 0);
	float3 dhdy = float3(0, TERRAIN_HEIGHT(stateTop) - TERRAIN_HEIGHT(stateBottom), 2 * _CellSize.y);
	float3 normal = cross(dhdx, dhdy);

	float sinTiltAngle = abs(normal.y) / length(normal);
	
	// Erosion limiting factor
	float lmax = saturate(1 - max(0, _MaxErosionDepth - WATER_HEIGHT(state)) / _MaxErosionDepth);
	float sedimentTransportCapacity = _SedimentCapacity * length(velocity) * min(sinTiltAngle, 0.05) * lmax;

	if (SEDIMENT(state) < sedimentTransportCapacity)
	{
		float mod = _TimeDelta * _SuspensionRate * HARDNESS(state) * (sedimentTransportCapacity - SEDIMENT(state));		
		TERRAIN_HEIGHT(state) -= mod;
		SEDIMENT(state) += mod;
		WATER_HEIGHT(state) += mod;
	}
	else
	{
		float mod = _TimeDelta * _DepositionRate * (SEDIMENT(state) - sedimentTransportCapacity);
		TERRAIN_HEIGHT(state) += mod;
		SEDIMENT(state) -= mod;
		WATER_HEIGHT(state) -= mod;
	}	

	// Water evaporation.
	WATER_HEIGHT(state) *= 1 - _Evaporation * _TimeDelta;
	 
	// Hardness update
	HARDNESS(state) = HARDNESS(state) - _TimeDelta * _SedimentSofteningRate * _SuspensionRate * (SEDIMENT(state) - sedimentTransportCapacity);
	HARDNESS(state) = clamp(HARDNESS(state), 0.1, 1);

	// Write heighmap
	CURRENT_SAMPLE(HeightMap) = state;
}