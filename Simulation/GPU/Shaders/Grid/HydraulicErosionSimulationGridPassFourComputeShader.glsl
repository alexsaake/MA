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
    float Hardness;

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

float SampleBilinear(vec2 uv)
{
	vec2 uva = floor(uv);
	vec2 uvb = ceil(uv);

	uvec2 id00 = uvec2(uva);  // 0 0
	uvec2 id10 = uvec2(uvb.x, uva.y); // 1 0
	uvec2 id01 = uvec2(uva.x, uvb.y); // 0 1	
	uvec2 id11 = uvec2(uvb); // 1 1

	vec2 d = uv - uva;

    return gridPoints[getIndex(id00)].SuspendedSediment * (1 - d.x) * (1 - d.y) +
		gridPoints[getIndex(id10)].SuspendedSediment * d.x * (1 - d.y) +
		gridPoints[getIndex(id01)].SuspendedSediment * (1 - d.x) * d.y +
		gridPoints[getIndex(id11)].SuspendedSediment * d.x * d.y;
}

void main()
{    
    float dt = 1.0;

    uint id = gl_GlobalInvocationID.x;
    uint myHeightMapSideLength = uint(sqrt(heightMap.length()));

    uint x = id % myHeightMapSideLength;
    uint y = id / myHeightMapSideLength;
    
    GridPoint gridPoint = gridPoints[id];

	// Sediment advection
	gridPoint.SuspendedSediment = SampleBilinear(vec2(x, y) - vec2(gridPoint.VelocityX, gridPoint.VelocityY) * dt);

    gridPoints[id] = gridPoint;
}