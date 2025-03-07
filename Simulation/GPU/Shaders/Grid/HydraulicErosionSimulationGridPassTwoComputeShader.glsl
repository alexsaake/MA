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

	float4 state = CURRENT_SAMPLE(HeightMap);
	float4 outputFlux = CURRENT_SAMPLE(FluxMap);
	float4 inputFlux = float4(
		RDIR(LEFT_SAMPLE(FluxMap)),
		LDIR(RIGHT_SAMPLE(FluxMap)),
		BDIR(TOP_SAMPLE(FluxMap)),
		TDIR(BOTTOM_SAMPLE(FluxMap)));
	float waterHeightBefore = WATER_HEIGHT(state);

	// Water surface and velocity field update
	// volume is changing by amount on incoming fluid volume minus outgoing
	float volumeDelta = SUM_COMPS(inputFlux) - SUM_COMPS(outputFlux);	

	// Then, we update the water height in the current (x, y) cell:
	WATER_HEIGHT(state) += _TimeDelta * volumeDelta / (_CellSize.x * _CellSize.y);	

	// Write new state to the HeightMap
	CURRENT_SAMPLE(HeightMap) = state;

	// Compute new velocity from flux to the VelocityMap
	CURRENT_SAMPLE(VelocityMap) = float2(
		0.5 * (LDIR(inputFlux) - LDIR(outputFlux) + RDIR(outputFlux) - RDIR(inputFlux)),
		0.5 * (BDIR(inputFlux) - BDIR(outputFlux) + TDIR(outputFlux) - TDIR(inputFlux)));
		/// _PipeLength * 0.5 * (waterHeightBefore + WATER_HEIGHT(state));
}