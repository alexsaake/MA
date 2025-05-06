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

layout(std430, binding = 1) readonly restrict buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct MapGenerationConfiguration
{
    float HeightMultiplier;
    float SeaLevel;
    float IsColorEnabled;
};

layout(std430, binding = 2) readonly restrict buffer MapGenerationConfigurationShaderBuffer
{
    MapGenerationConfiguration mapGenerationConfiguration;
};

uniform mat4 mvp;

vec4 oceanColor = vec4(0, 0.4, 0.8, 0.5);

void addVertex(uint vertex, uint x, uint y)
{
    float waterHeight = mapGenerationConfiguration.SeaLevel * mapGenerationConfiguration.HeightMultiplier;
    vec4 position = mvp * vec4(x, y, waterHeight, 1.0);

    gl_MeshVerticesNV[vertex].gl_Position = position;
    v_out[vertex].color = oceanColor;
}

void main()
{
    uint mapSize = uint(sqrt(heightMap.length()));

    addVertex(0, 0, 0);
    addVertex(1, mapSize, 0);
    addVertex(2, 0, mapSize);
    addVertex(3, mapSize, mapSize);
            
    gl_PrimitiveIndicesNV[0] = 0;
    gl_PrimitiveIndicesNV[1] = 1;
    gl_PrimitiveIndicesNV[2] = 3;
    gl_PrimitiveIndicesNV[3] = 0;
    gl_PrimitiveIndicesNV[4] = 3;
    gl_PrimitiveIndicesNV[5] = 2;

    gl_PrimitiveCountNV = PRIMITIVES;
}