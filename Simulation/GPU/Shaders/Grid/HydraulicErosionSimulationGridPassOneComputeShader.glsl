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

uint getIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

float GetFlowRight(uint x, uint y)
{
    if (x < 0 || x > myHeightMapSideLength - 1)
    {
        return 0.0;
    }
    return gridPoints[getIndex(x, y)].FlowRight;
}

float GetFlowLeft(uint x, uint y)
{
    if (x < 0 || x > myHeightMapSideLength - 1)
    {
        return 0.0;
    }
    return gridPoints[getIndex(x, y)].FlowLeft;
}

float GetFlowBottom(uint x, uint y)
{
    if (y < 0 || y > myHeightMapSideLength - 1)
    {
        return 0.0;
    }
    return gridPoints[getIndex(x, y)].FlowBottom;
}

float GetFlowTop(uint x, uint y)
{
    if (y < 0 || y > myHeightMapSideLength - 1)
    {
        return 0.0;
    }
    return gridPoints[getIndex(x, y)].FlowTop;
}

float Kdmax = 10.0;

float lmax(float waterHeight)
{
    if(waterHeight <= 0)
    {
        return 0;
    }
    if(waterHeight >= Kdmax)
    {
        return 1;
    }
    return 1 - (Kdmax - waterHeight) / Kdmax;
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute

void main()
{
    float dt = 0.25;
    float dx = 1.0;
    float dy = 1.0;

    uint id = gl_GlobalInvocationID.x;
    myHeightMapSideLength = uint(sqrt(gridPoints.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridPoint gridPoint = gridPoints[id];

	// Sample the heighmap (state map)
	float4 state = CURRENT_SAMPLE(HeightMap);
	float4 stateLeft = LEFT_SAMPLE(HeightMap);
	float4 stateRight = RIGHT_SAMPLE(HeightMap);
	float4 stateTop = TOP_SAMPLE(HeightMap);
	float4 stateBottom = BOTTOM_SAMPLE(HeightMap);

	float terrainHeight = TERRAIN_HEIGHT(state);
	float waterHeight = WATER_HEIGHT(state);

	// Flow simulation using shallow-water model. Computation of the velocity field and water height changes.
	// Sample flux
	float4 outputFlux = CURRENT_SAMPLE(FluxMap);

	// Overall height difference in each direction
	float4 heightDifference = FULL_HEIGHT(state) - float4(
		FULL_HEIGHT(stateLeft),
		FULL_HEIGHT(stateRight),
		FULL_HEIGHT(stateTop),
		FULL_HEIGHT(stateBottom));

	// Output flux	
	outputFlux = max(0, outputFlux + _TimeDelta * _Gravity * _PipeArea * heightDifference / _PipeLength);

	/*
		Rescale flux
		The total outflow should not exceed the total amount
		of the water in the given cell.If the calculated value is
		larger than the current amount in the given cell, then flux will
		be scaled down with an appropriate factor
	*/
	outputFlux *= min(1, waterHeight * _CellSize.x * _CellSize.y / (SUM_COMPS(outputFlux) * _TimeDelta));

	// Boundaries (uncomment thisif you want water to bounce of boundaries)						
	if (id.x == 0) LDIR(outputFlux) = 0;
	if (id.y == 0) BDIR(outputFlux) = 0;
	if (id.x == _Width - 1) RDIR(outputFlux) = 0;
	if (id.y == _Height - 1) TDIR(outputFlux) = 0;	

	// Write new flux to the FluxMap
	CURRENT_SAMPLE(FluxMap) = max(0, outputFlux);
}