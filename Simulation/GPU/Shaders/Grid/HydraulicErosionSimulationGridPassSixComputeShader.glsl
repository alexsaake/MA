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

void main()
{    
    float dt = 0.25;

    uint id = gl_GlobalInvocationID.x;
    uint myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridPoint gridPoint = gridPoints[id];

	float4 state = CURRENT_SAMPLE(HeightMap);
	float4 outputFlux = CURRENT_SAMPLE(TerrainFluxMap);
	float4 inputFlux = float4(
		RDIR(LEFT_SAMPLE(TerrainFluxMap)),
		LDIR(RIGHT_SAMPLE(TerrainFluxMap)),
		BDIR(TOP_SAMPLE(TerrainFluxMap)),
		TDIR(BOTTOM_SAMPLE(TerrainFluxMap)));	
	
	// Volume is changing by amount on incoming mass minus outgoing mass
	float volumeDelta = SUM_COMPS(inputFlux) - SUM_COMPS(outputFlux);

	// Then, we update the terrain height in the current (x, y) cell
	// min - is to prevent addind more mass than in flux
	TERRAIN_HEIGHT(state) += min(1, _TimeDelta * _ThermalErosionTimeScale) * volumeDelta;

	// Write new state to the HeightMap
	CURRENT_SAMPLE(HeightMap) = state;
}