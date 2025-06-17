//https://www.geeks3d.com/hacklab/20200515/demo-rgb-triangle-with-mesh-shaders-in-opengl/

#version 450
#extension GL_NV_mesh_shader : enable

layout(local_size_x = 1) in;

#define VERTICES 64
#define PRIMITIVES 98

layout(max_vertices = VERTICES, max_primitives = PRIMITIVES) out;

layout(triangles) out;

out PerVertexData
{
	vec4 position;
	vec4 color;
	vec4 normal;
} v_out[];

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
    bool AreLayerColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer mapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

uint myHeightMapPlaneSize;
uint myHeightMapSideLength;

uint GetIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

uint LayerHydraulicErosionCellsOffset(uint layer)
{
    return layer * myHeightMapPlaneSize;
}

float TotalSuspendedSediment(uint index)
{    
    float suspendedSediment = 0.0;
    for(int layer = 0; layer < mapGenerationConfiguration.LayerCount; layer++)
    {
        suspendedSediment += gridHydraulicErosionCells[index + LayerHydraulicErosionCellsOffset(layer)].SuspendedSediment;
    }
    return suspendedSediment;
}

float HeightMapLayerFloorHeight(uint index, uint layer)
{
    if(layer < 1
        || layer >= mapGenerationConfiguration.LayerCount)
    {
        return 0.0;
    }
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

uint HeightMapLayerOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

uint HeightMapRockTypeOffset(uint rockType)
{
    return rockType * myHeightMapPlaneSize;
}

float HeightMapLayerHeight(uint index, uint layer)
{
    float heightMapLayerHeight = 0.0;
    for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
    {
        heightMapLayerHeight += heightMap[index + HeightMapRockTypeOffset(rockType) + HeightMapLayerOffset(layer)];
    }
    return heightMapLayerHeight;
}

float TotalHeightMapHeight(uint index)
{
    float heightMapLayerFloorHeight = 0.0;
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        heightMapLayerFloorHeight = 0.0;
        if(layer > 0)
        {
            heightMapLayerFloorHeight = HeightMapLayerFloorHeight(index, uint(layer));
            if(heightMapLayerFloorHeight == 0)
            {
                continue;
            }
        }
        float heightMapLayerHeight = HeightMapLayerHeight(index, uint(layer));
        if(heightMapLayerHeight > 0)
        {
            return heightMapLayerFloorHeight + heightMapLayerHeight;
        }
    }
    return 0.0;
}

uniform mat4 mvp;

vec4 sedimentColor = vec4(0.3, 0.2, 0.1, 0.5);

void AddVertex(uint vertex, uint x, uint y)
{
    uint index = GetIndex(x, y);
    float zOffset = 0.00004;
    vec4 position = mvp * vec4(x, y, (TotalHeightMapHeight(index) - zOffset + TotalSuspendedSediment(index)) * mapGenerationConfiguration.HeightMultiplier, 1.0);

    gl_MeshVerticesNV[vertex].gl_Position = position;
    v_out[vertex].position = position;
    v_out[vertex].color = sedimentColor;
    v_out[vertex].normal = vec4(0.0, 0.0, 1.0, 1.0);
}

void main()
{
    uint threadNumber = gl_GlobalInvocationID.x;
    myHeightMapPlaneSize = heightMap.length() / (mapGenerationConfiguration.RockTypeCount * mapGenerationConfiguration.LayerCount + mapGenerationConfiguration.LayerCount - 1);
    if(threadNumber >= myHeightMapPlaneSize)
    {
        return;
    }
    myHeightMapSideLength = uint(sqrt(myHeightMapPlaneSize));
    uint meshletSize = uint(sqrt(VERTICES));
    uint yMeshletCount = uint(ceil(float(myHeightMapSideLength) / (meshletSize - 1)));
    uint xOffset = uint(floor(threadNumber / yMeshletCount)) * (meshletSize - 1);
    uint yOffset = (threadNumber % yMeshletCount) * (meshletSize - 1);

    if(xOffset + (meshletSize - 1) > myHeightMapSideLength || yOffset + (meshletSize - 1) > myHeightMapSideLength)
    {
        return;
    }

    uint vertex = 0;
    uint index = 0;
    for(uint y = 0; y < meshletSize - 1; y+=2)
    {
        for(uint x = 0; x < meshletSize - 1; x+=2)
        {
            AddVertex(vertex + 0, x + xOffset, y + yOffset);
            AddVertex(vertex + 1, x + 1 + xOffset, y + yOffset);
            AddVertex(vertex + 2, x + xOffset, y + 1 + yOffset);
            AddVertex(vertex + 3, x + 1 + xOffset, y + 1 + yOffset);
            
            gl_PrimitiveIndicesNV[index + 0] = vertex + 0;
            gl_PrimitiveIndicesNV[index + 1] = vertex + 1;
            gl_PrimitiveIndicesNV[index + 2] = vertex + 3;
            gl_PrimitiveIndicesNV[index + 3] = vertex + 0;
            gl_PrimitiveIndicesNV[index + 4] = vertex + 3;
            gl_PrimitiveIndicesNV[index + 5] = vertex + 2;
            index += 6;

            if(x > 0)
            {
                gl_PrimitiveIndicesNV[index + 0] = vertex - 3;
                gl_PrimitiveIndicesNV[index + 1] = vertex + 0;
                gl_PrimitiveIndicesNV[index + 2] = vertex + 2;
                gl_PrimitiveIndicesNV[index + 3] = vertex - 3;
                gl_PrimitiveIndicesNV[index + 4] = vertex + 2;
                gl_PrimitiveIndicesNV[index + 5] = vertex - 1;
                index += 6;

                if(y > 0)
                {
                    gl_PrimitiveIndicesNV[index + 0] = vertex - 17;
                    gl_PrimitiveIndicesNV[index + 1] = vertex - 14;
                    gl_PrimitiveIndicesNV[index + 2] = vertex + 0;
                    gl_PrimitiveIndicesNV[index + 3] = vertex - 17;
                    gl_PrimitiveIndicesNV[index + 4] = vertex + 0;
                    gl_PrimitiveIndicesNV[index + 5] = vertex - 3;
                    index += 6;
                }
            }

            if(y > 0)
            {
                gl_PrimitiveIndicesNV[index + 0] = vertex - 14;
                gl_PrimitiveIndicesNV[index + 1] = vertex - 13;
                gl_PrimitiveIndicesNV[index + 2] = vertex + 1;
                gl_PrimitiveIndicesNV[index + 3] = vertex - 14;
                gl_PrimitiveIndicesNV[index + 4] = vertex + 1;
                gl_PrimitiveIndicesNV[index + 5] = vertex + 0;
                index += 6;
            }

            vertex += 4;
        }
    }

    gl_PrimitiveCountNV = PRIMITIVES;
}