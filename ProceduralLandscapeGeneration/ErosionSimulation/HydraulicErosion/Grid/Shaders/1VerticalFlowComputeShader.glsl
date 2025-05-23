#version 430

layout (local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(std430, binding = 0) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct GridHydraulicErosionCell
{
    float WaterHeight;

    float WaterFlowLeft;
    float WaterFlowRight;
    float WaterFlowUp;
    float WaterFlowDown;

    float SuspendedSediment;

    float SedimentFlowLeft;
    float SedimentFlowRight;
    float SedimentFlowUp;
    float SedimentFlowDown;

    vec2 Velocity;
};

layout(std430, binding = 4) buffer gridHydraulicErosionCellShaderBuffer
{
    GridHydraulicErosionCell[] gridHydraulicErosionCells;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    uint RockTypeCount;
    uint LayerCount;
    float SeaLevel;
    bool AreTerrainColorsEnabled;
    bool ArePlateTectonicsPlateColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

struct ErosionConfiguration
{
    float TimeDelta;
	bool IsWaterKeptInBoundaries;
};

layout(std430, binding = 6) readonly restrict buffer erosionConfigurationShaderBuffer
{
    ErosionConfiguration erosionConfiguration;
};

struct GridErosionConfiguration
{
    float WaterIncrease;
    float Gravity;
    float Dampening;
    float MaximalErosionDepth;
    float SedimentCapacity;
    float SuspensionRate;
    float DepositionRate;
    float EvaporationRate;
};

layout(std430, binding = 9) buffer gridErosionConfigurationShaderBuffer
{
    GridErosionConfiguration gridErosionConfiguration;
};

uint myHeightMapSideLength;
uint myHeightMapLength;

float TotalHeight(uint index, uint layer)
{
    float height = 0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        height += heightMap[index + rockType * myHeightMapLength + (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapLength];
    }
    return height;
}

uint GetIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

//https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute
//https://github.com/GuilBlack/Erosion/blob/master/Assets/Resources/Shaders/ComputeErosion.compute
//https://lisyarus.github.io/blog/posts/simulating-water-over-terrain.html
//https://github.com/karhu/terrain-erosion/blob/master/Simulation/FluidSimulation.cpp
//damping
//https://github.com/patiltanma/15618-FinalProject/blob/master/Renderer/Renderer/erosion_kernel.cu
//adding sediment flow
//https://github.com/Clocktown/CUDA-3D-Hydraulic-Erosion-Simulation-with-Layered-Stacks/blob/main/core/geo/device/transport.cu

void main()
{
    uint index = gl_GlobalInvocationID.x;
    myHeightMapLength = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(index >= myHeightMapLength)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapLength));

    uint x = index % myHeightMapSideLength;
    uint y = index / myHeightMapSideLength;

    for(uint layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        uint gridHydraulicErosionCellIndexOffset = layer * myHeightMapLength;
        GridHydraulicErosionCell gridHydraulicErosionCell  = gridHydraulicErosionCells[index + gridHydraulicErosionCellIndexOffset];
    
        float height = TotalHeight(index, layer);
        if(height < mapGenerationConfiguration.SeaLevel)
        {
            gridHydraulicErosionCell.WaterHeight = mapGenerationConfiguration.SeaLevel - height;
        }
        float totalHeight = (height + gridHydraulicErosionCell.WaterHeight) * mapGenerationConfiguration.HeightMultiplier;
        float outOfBoundsHeight = totalHeight - 0.2;
        if(x > 0)
        {
            uint leftIndex = GetIndex(x - 1, y);
            float totalHeightLeft = (TotalHeight(leftIndex, layer) + gridHydraulicErosionCells[leftIndex + gridHydraulicErosionCellIndexOffset].WaterHeight) * mapGenerationConfiguration.HeightMultiplier;
            gridHydraulicErosionCell.WaterFlowLeft = max(gridHydraulicErosionCell.WaterFlowLeft + (totalHeight - totalHeightLeft) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            gridHydraulicErosionCell.SedimentFlowLeft = max(gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightLeft) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        }
        else
        {
            if(erosionConfiguration.IsWaterKeptInBoundaries)
            {
                gridHydraulicErosionCell.WaterFlowLeft = 0.0;
                gridHydraulicErosionCell.SedimentFlowLeft = 0.0;
            }
            else
            {
                gridHydraulicErosionCell.WaterFlowLeft = max(gridHydraulicErosionCell.WaterFlowLeft + (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
                gridHydraulicErosionCell.SedimentFlowLeft = max(gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            }
        }

        if(x < myHeightMapSideLength - 1)
        {
            uint rightIndex = GetIndex(x + 1, y);
            float totalHeightRight = (TotalHeight(rightIndex, layer) + gridHydraulicErosionCells[rightIndex + gridHydraulicErosionCellIndexOffset].WaterHeight) * mapGenerationConfiguration.HeightMultiplier;
            gridHydraulicErosionCell.WaterFlowRight = max(gridHydraulicErosionCell.WaterFlowRight + (totalHeight - totalHeightRight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            gridHydraulicErosionCell.SedimentFlowRight = max(gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightRight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        }
        else
        {
            if(erosionConfiguration.IsWaterKeptInBoundaries)
            {
                gridHydraulicErosionCell.WaterFlowRight = 0.0;
                gridHydraulicErosionCell.SedimentFlowRight = 0.0;
            }
            else
            {
                gridHydraulicErosionCell.WaterFlowRight = max(gridHydraulicErosionCell.WaterFlowRight + (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
                gridHydraulicErosionCell.SedimentFlowRight = max(gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            }
        }

        if(y > 0)
        {
            uint downIndex = GetIndex(x, y - 1);
            float totalHeightDown = (TotalHeight(downIndex, layer) + gridHydraulicErosionCells[downIndex + gridHydraulicErosionCellIndexOffset].WaterHeight) * mapGenerationConfiguration.HeightMultiplier;
            gridHydraulicErosionCell.WaterFlowDown = max(gridHydraulicErosionCell.WaterFlowDown + (totalHeight - totalHeightDown) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            gridHydraulicErosionCell.SedimentFlowDown = max(gridHydraulicErosionCell.SedimentFlowDown + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightDown) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        }
        else
        {
            if(erosionConfiguration.IsWaterKeptInBoundaries)
            {
                gridHydraulicErosionCell.WaterFlowDown = 0.0;
                gridHydraulicErosionCell.SedimentFlowDown = 0.0;
            }
            else
            {
                gridHydraulicErosionCell.WaterFlowDown = max(gridHydraulicErosionCell.WaterFlowDown + (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
                gridHydraulicErosionCell.SedimentFlowDown = max(gridHydraulicErosionCell.SedimentFlowDown + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            }
        }

        if(y < myHeightMapSideLength - 1)
        {
            uint upIndex = GetIndex(x, y + 1);
            float totalHeightUp = (TotalHeight(upIndex, layer) + gridHydraulicErosionCells[upIndex + gridHydraulicErosionCellIndexOffset].WaterHeight) * mapGenerationConfiguration.HeightMultiplier;
            gridHydraulicErosionCell.WaterFlowUp = max(gridHydraulicErosionCell.WaterFlowUp + (totalHeight - totalHeightUp) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            gridHydraulicErosionCell.SedimentFlowUp = max(gridHydraulicErosionCell.SedimentFlowUp + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - totalHeightUp) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
        }
        else
        {
            if(erosionConfiguration.IsWaterKeptInBoundaries)
            {
                gridHydraulicErosionCell.WaterFlowUp = 0.0;
                gridHydraulicErosionCell.SedimentFlowUp = 0.0;
            }
            else
            {
                gridHydraulicErosionCell.WaterFlowUp = max(gridHydraulicErosionCell.WaterFlowUp + (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
                gridHydraulicErosionCell.SedimentFlowUp = max(gridHydraulicErosionCell.SedimentFlowUp + gridHydraulicErosionCell.SuspendedSediment * (totalHeight - outOfBoundsHeight) * gridErosionConfiguration.Gravity * erosionConfiguration.TimeDelta, 0.0);
            }
        }

        float totalFlow = gridHydraulicErosionCell.WaterFlowLeft + gridHydraulicErosionCell.WaterFlowRight + gridHydraulicErosionCell.WaterFlowDown + gridHydraulicErosionCell.WaterFlowUp;
        float scale = min(gridHydraulicErosionCell.WaterHeight * mapGenerationConfiguration.HeightMultiplier / totalFlow * erosionConfiguration.TimeDelta * (1.0 - gridErosionConfiguration.Dampening), 1.0);        
        gridHydraulicErosionCell.WaterFlowLeft *= scale;
        gridHydraulicErosionCell.WaterFlowRight *= scale;
        gridHydraulicErosionCell.WaterFlowDown *= scale;
        gridHydraulicErosionCell.WaterFlowUp *= scale;
    
        float totalSedimentFlow = gridHydraulicErosionCell.SedimentFlowLeft + gridHydraulicErosionCell.SedimentFlowRight + gridHydraulicErosionCell.SedimentFlowDown + gridHydraulicErosionCell.SedimentFlowUp;
        float sedimentScale = min(gridHydraulicErosionCell.SuspendedSediment * mapGenerationConfiguration.HeightMultiplier / totalSedimentFlow * erosionConfiguration.TimeDelta * (1.0 - gridErosionConfiguration.Dampening), 1.0);
        gridHydraulicErosionCell.SedimentFlowLeft *= sedimentScale;
        gridHydraulicErosionCell.SedimentFlowRight *= sedimentScale;
        gridHydraulicErosionCell.SedimentFlowDown *= sedimentScale;
        gridHydraulicErosionCell.SedimentFlowUp *= sedimentScale;

        gridHydraulicErosionCells[index + gridHydraulicErosionCellIndexOffset] = gridHydraulicErosionCell;
    }
    
    memoryBarrier();
}