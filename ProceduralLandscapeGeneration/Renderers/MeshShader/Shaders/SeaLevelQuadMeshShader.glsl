//https://www.geeks3d.com/hacklab/20200515/demo-rgb-triangle-with-mesh-shaders-in-opengl/

#version 450
#extension GL_NV_mesh_shader : enable

layout(local_size_x = 1) in;

#define VERTICES 4
#define PRIMITIVES 6

layout(max_vertices = VERTICES, max_primitives = PRIMITIVES) out;

layout(triangles) out;

out PerVertexData
{
	vec4 color;
} v_out[];

layout(std430, binding = 0) readonly restrict buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    uint LayerCount;
    bool AreTerrainColorsEnabled;
    bool ArePlateTectonicsPlateColorsEnabled;
};

layout(std430, binding = 5) readonly restrict buffer MapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

struct ErosionConfiguration
{
    float SeaLevel;
    float TimeDelta;
};

layout(std430, binding = 6) readonly restrict buffer erosionConfigurationShaderBuffer
{
    ErosionConfiguration erosionConfiguration;
};

uniform mat4 mvp;

vec4 oceanColor = vec4(0, 0.4, 0.8, 0.5);

void addVertex(uint vertex, uint x, uint y)
{
    float seaLevelHeight = erosionConfiguration.SeaLevel * mapGenerationConfiguration.HeightMultiplier;
    vec4 position = mvp * vec4(x, y, seaLevelHeight, 1.0);

    gl_MeshVerticesNV[vertex].gl_Position = position;
    v_out[vertex].color = oceanColor;
}

void main()
{
    uint heightMapLength = heightMap.length() / mapGenerationConfiguration.LayerCount;
    uint mapSize = uint(sqrt(heightMapLength));

    addVertex(0, 0, 0);
    addVertex(1, mapSize, 0);
    addVertex(2, mapSize, mapSize);
    addVertex(3, 0, mapSize);
            
    gl_PrimitiveIndicesNV[0] = 0;
    gl_PrimitiveIndicesNV[1] = 1;
    gl_PrimitiveIndicesNV[2] = 2;
    gl_PrimitiveIndicesNV[3] = 0;
    gl_PrimitiveIndicesNV[4] = 2;
    gl_PrimitiveIndicesNV[5] = 3;

    gl_PrimitiveCountNV = PRIMITIVES;
}