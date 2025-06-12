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

struct PlateTectonicsSegment
{
    int Plate;
    float Mass;
    float Inertia;
    float Density;
    float Height;
    float Thickness;
    bool IsAlive;
    bool IsColliding;
    vec2 Position;
};

layout(std430, binding = 15) buffer plateTectonicsSegmentsShaderBuffer
{
    PlateTectonicsSegment[] plateTectonicsSegments;
};

uniform mat4 mvp;

uint myHeightMapSideLength;
uint myHeightMapPlaneSize;

uint LayerOffset(uint layer)
{
    return (layer * mapGenerationConfiguration.RockTypeCount + layer) * myHeightMapPlaneSize;
}

float FineSedimentHeight(uint index)
{
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        float height = heightMap[index + (mapGenerationConfiguration.RockTypeCount - 1) * myHeightMapPlaneSize + LayerOffset(layer)];
        if(height > 0)
        {
            return height;
        }
    }
    return 0;
}

float CoarseSedimentHeight(uint index)
{
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
        float height = heightMap[index + 1 * myHeightMapPlaneSize + LayerOffset(layer)];
        if(height > 0)
        {
            return height;
        }
    }
    return 0;
}

float HeightMapFloorHeight(uint index, uint layer)
{
    if(layer < 1)
    {
        return 0.0;
    }
    return heightMap[index + layer * mapGenerationConfiguration.RockTypeCount * myHeightMapPlaneSize];
}

float TotalHeightMapHeight(uint index)
{
    float heightMapFloorHeight = 0.0;
    float rockTypeHeight = 0.0;
    for(int layer = int(mapGenerationConfiguration.LayerCount) - 1; layer >= 0; layer--)
    {
		heightMapFloorHeight = 0.0;
        if(layer > 0)
        {
            heightMapFloorHeight = HeightMapFloorHeight(index, layer);
            if(heightMapFloorHeight == 0)
            {
                continue;
            }
        }
        for(uint rockType = 0; rockType < mapGenerationConfiguration.RockTypeCount; rockType++)
        {
            rockTypeHeight += heightMap[index + rockType * myHeightMapPlaneSize + LayerOffset(layer)];
        }
        if(rockTypeHeight > 0)
        {
            return heightMapFloorHeight + rockTypeHeight;
        }
    }
    return heightMapFloorHeight + rockTypeHeight;
}

uint GetIndex(uint x, uint y)
{
    return (y * myHeightMapSideLength) + x;
}

vec3 GetScaledNormal(uint x, uint y)
{
    if (x < 1 || x > myHeightMapSideLength - 2
        || y < 1 || y > myHeightMapSideLength - 2)
    {
        return vec3(0.0, 0.0, 1.0);
    }

    float rb = TotalHeightMapHeight(GetIndex(x + 1, y - 1));
    float lb = TotalHeightMapHeight(GetIndex(x - 1, y - 1));
    float r = TotalHeightMapHeight(GetIndex(x + 1, y));
    float l = TotalHeightMapHeight(GetIndex(x - 1, y));
    float rt = TotalHeightMapHeight(GetIndex(x + 1, y + 1));
    float lt = TotalHeightMapHeight(GetIndex(x - 1, y + 1));
    float t = TotalHeightMapHeight(GetIndex(x, y + 1));
    float b = TotalHeightMapHeight(GetIndex(x, y - 1));

    vec3 normal = vec3(
    mapGenerationConfiguration.HeightMultiplier * -(rb - lb + 2 * (r - l) + rt - lt),
    mapGenerationConfiguration.HeightMultiplier * -(lt - lb + 2 * (t - b) + rt - rb),
    1.0);

    return normalize(normal);
}

vec3 oceanCliff = vec3(0.2, 0.2, 0.1);
vec3 beachColor = vec3(1.0, 0.9, 0.6);
vec3 pastureColor = vec3(0.5, 0.6, 0.4);
vec3 woodsColor = vec3(0.2, 0.3, 0.2);
vec3 mountainColor = vec3(0.6, 0.6, 0.6);
vec3 snowColor = vec3(1.0, 0.9, 0.9);

vec3 bedrockColor = mountainColor;
vec3 coarseSedimentColor = vec3(0.5, 0.3, 0.3);
vec3 fineSedimentColor = beachColor;

void addVertex(uint vertex, uint x, uint y)
{
    uint index = GetIndex(x, y);
    float height = TotalHeightMapHeight(index);
    float terrainHeight = height * mapGenerationConfiguration.HeightMultiplier;
    float seaLevelHeight = mapGenerationConfiguration.SeaLevel * mapGenerationConfiguration.HeightMultiplier;
    vec3 normal = GetScaledNormal(x, y);
    vec4 position = mvp * vec4(x, y, terrainHeight, 1.0);

    gl_MeshVerticesNV[vertex].gl_Position = position;
    v_out[vertex].position = position;
    
    vec3 terrainColor = vec3(1.0);
    if(plateTectonicsSegments.length() > 0
        && mapGenerationConfiguration.ArePlateTectonicsPlateColorsEnabled)
    {
        int plate = plateTectonicsSegments[index].Plate;
        if(plate == 0)
        {
            terrainColor = vec3(1, 0, 0);
        }
        if(plate == 1)
        {
            terrainColor = vec3(0, 1, 0);
        }
        if(plate == 2)
        {
            terrainColor = vec3(0, 0, 1);
        }
        if(plate == 3)
        {
            terrainColor = vec3(1, 0, 1);
        }
        if(plate == 4)
        {
            terrainColor = vec3(0, 1, 1);
        }
        if(plate == 5)
        {
            terrainColor = vec3(1, 1, 0);
        }
        if(plate == 6)
        {
            terrainColor = vec3(0.5, 0, 0);
        }
        if(plate == 7)
        {
            terrainColor = vec3(0, 0.5, 0);
        }
        if(plate == 8)
        {
            terrainColor = vec3(0, 0, 0.5);
        }
        if(plate == 9)
        {
            terrainColor = vec3(0.5, 0, 0.5);
        }
    }
    else if(mapGenerationConfiguration.AreTerrainColorsEnabled)
    {
        if(mapGenerationConfiguration.RockTypeCount > 1)
        {
            if(FineSedimentHeight(index) > 0.00001)
            {
                terrainColor = fineSedimentColor;
            }
            else if(mapGenerationConfiguration.RockTypeCount > 2
                && CoarseSedimentHeight(index) > 0.00001)
            {
                terrainColor = coarseSedimentColor;
            }
            else
            {
                terrainColor = bedrockColor;
            }
        }
        else
        {
            if(terrainHeight < seaLevelHeight + 0.3)
            {
                if(normal.z > 0.3)
                {
                    terrainColor = beachColor;
                }
                else
                {
                    terrainColor = oceanCliff;
                }
            }
            else
            {
                if(normal.z > 0.4)
                {
                    if(height > 0.9)
                    {
                        terrainColor = snowColor;
                    }
                    else if(height > 0.7)
                    {
                        terrainColor = mountainColor;
                    }
                    else
                    {
                        terrainColor = woodsColor;
                    }
                }
                else if(normal.z > 0.3)
                {
                    if(height > 0.9)
                    {
                        terrainColor = snowColor;
                    }
                    else if(height > 0.8)
                    {
                        terrainColor = mountainColor;
                    }
                    else
                    {
                        terrainColor = pastureColor;
                    }
                }
                else
                {
                    terrainColor = mountainColor;
                }
            }
        }
    }

    v_out[vertex].color = vec4(terrainColor, 1.0);
    v_out[vertex].normal = vec4(normal, 1.0);
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
            addVertex(vertex + 0, x + xOffset, y + yOffset);
            addVertex(vertex + 1, x + 1 + xOffset, y + yOffset);
            addVertex(vertex + 2, x + xOffset, y + 1 + yOffset);
            addVertex(vertex + 3, x + 1 + xOffset, y + 1 + yOffset);
            
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