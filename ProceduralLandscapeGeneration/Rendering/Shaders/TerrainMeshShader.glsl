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

layout(std430, binding = 1) readonly restrict buffer heightMapShaderBuffer
{
    float[] heightMap;
};

struct Configuration
{
    float HeightMultiplier;
    float SeaLevel;
};

layout(std430, binding = 2) readonly restrict buffer configurationShaderBuffer
{
    Configuration configuration;
};

uniform mat4 mvp;

uint myMapSize;

uint getIndex(uint x, uint y)
{
    return (y * myMapSize) + x;
}

vec3 getScaledNormal(uint x, uint y)
{
    if (x < 1 || x > myMapSize - 2
        || y < 1 || y > myMapSize - 2)
    {
        return vec3(0.0, 0.0, 1.0);
    }

    float xp1ym1 = heightMap[getIndex(x + 1, y - 1)];
    float xm1ym1 = heightMap[getIndex(x - 1, y - 1)];
    float xp1y = heightMap[getIndex(x + 1, y)];
    float xm1y = heightMap[getIndex(x - 1, y)];
    float xp1yp1 = heightMap[getIndex(x + 1, y + 1)];
    float xm1yp1 = heightMap[getIndex(x - 1, y + 1)];
    float xyp1 = heightMap[getIndex(x, y + 1)];
    float xym1 = heightMap[getIndex(x, y - 1)];

    vec3 normal = vec3(
    configuration.HeightMultiplier * -(xp1ym1 - xm1ym1 + 2 * (xp1y - xm1y) + xp1yp1 - xm1yp1),
    configuration.HeightMultiplier * -(xm1yp1 - xm1ym1 + 2 * (xyp1 - xym1) + xp1yp1 - xp1ym1),
    1.0);

    return normalize(normal);
}

void addVertex(uint vertex, uint x, uint y)
{
    uint index = getIndex(x, y);
    vec4 position = mvp * vec4(x, y, heightMap[index] * configuration.HeightMultiplier, 1.0);

    gl_MeshVerticesNV[vertex].gl_Position = position;
    v_out[vertex].position = position;
    v_out[vertex].color = vec4(1.0, 1.0, 1.0, 1.0);
    v_out[vertex].normal = vec4(getScaledNormal(x, y), 1.0);
}

void main()
{
    uint threadNumber = gl_GlobalInvocationID.x;
    myMapSize = uint(sqrt(heightMap.length()));
    uint meshletSize = uint(sqrt(VERTICES));
    uint yMeshletCount = uint(ceil(float(myMapSize) / (meshletSize - 1)));
    uint xOffset = uint(floor(threadNumber / yMeshletCount)) * (meshletSize - 1);
    uint yOffset = (threadNumber % yMeshletCount) * (meshletSize - 1);

    if(xOffset + (meshletSize - 1) > myMapSize || yOffset + (meshletSize - 1) > myMapSize)
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