#version 430

layout(std430, binding = 0) readonly restrict buffer heightMapShaderBuffer
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

    vec2 WaterVelocity;
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

in vec3 vertexPosition;
in vec4 vertexColor;
in vec3 vertexNormal;
in vec2 vertexTexCoords;

uniform mat4 mvp;

out vec4 fragColor;

vec4 waterColor = vec4(0.0, 0.0, 1.0, 0.25);

uint myHeightMapPlaneSize;

float LayerHeightMapFloorHeight(uint index, uint layer)
{
    if(layer < 1)
    {
        return 0.0;
    }
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

uint LayerHeightMapOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

float LayerHeightMapRockTypeHeight(uint index, uint layer)
{
    float layerHeightMapRockTypeHeight = 0.0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        layerHeightMapRockTypeHeight += heightMap[index + rockType * myHeightMapPlaneSize + LayerHeightMapOffset(layer)];
    }
    return layerHeightMapRockTypeHeight;
}

float TotalLayerHeightMapHeight(uint index, uint layer)
{
    float layerHeightMapFloorHeight = 0.0;
    if(layer > 0)
    {
        layerHeightMapFloorHeight = LayerHeightMapFloorHeight(index, layer);
        if(layerHeightMapFloorHeight == 0)
        {
            return 0.0;
        }
    }
    return layerHeightMapFloorHeight + LayerHeightMapRockTypeHeight(index, layer);
}

void main()
{
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);

    uint gridHydraulicErosionCellIndex = uint(vertexTexCoords.x);
    uint topBottom = uint(vertexTexCoords.y);
    uint layerOffset = myHeightMapPlaneSize;
    uint layer = uint(gridHydraulicErosionCellIndex * 1.0 / layerOffset);
    uint baseIndex = gridHydraulicErosionCellIndex - layer * layerOffset;
    float height = 0.0;
    float totalLayerHeightMapHeight = TotalLayerHeightMapHeight(baseIndex, layer);
    if(gridHydraulicErosionCells[gridHydraulicErosionCellIndex].WaterHeight > 0)
    {
        if(topBottom == 0)
        {
            height = totalLayerHeightMapHeight + gridHydraulicErosionCells[gridHydraulicErosionCellIndex].WaterHeight;
        }
        else
        {
            height = totalLayerHeightMapHeight;
        }
    }
    float waterHeight = height * mapGenerationConfiguration.HeightMultiplier;

    fragColor = waterColor;
    gl_Position =  mvp * vec4(vertexPosition.xy, waterHeight, 1.0);
}