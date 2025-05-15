#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct GridHydraulicErosionCell
{
    float WaterHeight;
    float SuspendedSediment;
    float TempSediment;
    float FlowLeft;
    float FlowRight;
    float FlowTop;
    float FlowBottom;
    vec2 Velocity;
};

layout(std430, binding = 4) buffer gridHydraulicErosionCellShaderBuffer
{
    GridHydraulicErosionCell[] gridHydraulicErosionCells;
};

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute

void main()
{
    uint id = gl_GlobalInvocationID.x;
    uint gridHydraulicErosionCellsLength = gridHydraulicErosionCells.length();
    if(id > gridHydraulicErosionCellsLength)
    {
        return;
    }

    GridHydraulicErosionCell gridHydraulicErosionCell = gridHydraulicErosionCells[id];

    gridHydraulicErosionCell.SuspendedSediment = gridHydraulicErosionCell.TempSediment;

    gridHydraulicErosionCells[id] = gridHydraulicErosionCell;
    
    memoryBarrier();
}