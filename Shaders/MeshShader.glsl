//https://www.geeks3d.com/hacklab/20200515/demo-rgb-triangle-with-mesh-shaders-in-opengl/

#version 450
#extension GL_NV_mesh_shader : enable

layout(local_size_x=1) in;

#define VERTICES 64
#define PRIMITIVES 98

layout(max_vertices=VERTICES, max_primitives=PRIMITIVES) out;

layout(triangles) out;

out PerVertexData
{
  vec4 color;
} v_out[];

layout(std430, binding = 1) readonly restrict buffer heightMapShaderBuffer
{
    float[] heightMap;
};

uniform mat4 mvp;

uint getIndex(uint x, uint y)
{
    uint size = uint(sqrt(heightMap.length()));
    return (y * size) + x;
}

//private Vector3 GetScaledNormal(int x, int y, int scale)
//{
//    if (x < 1 || x > Width - 2
//        || y < 1 || y > Depth - 2)
//    {
//        return new Vector3(0, 0, 1);
//    }

//    Vector3 normal = new(
//    scale * -(Height[x + 1, y - 1] - Height[x - 1, y - 1] + 2 * (Height[x + 1, y] - Height[x - 1, y]) + Height[x + 1, y + 1] - Height[x - 1, y + 1]),
//    scale * -(Height[x - 1, y + 1] - Height[x - 1, y - 1] + 2 * (Height[x, y + 1] - Height[x, y - 1]) + Height[x + 1, y + 1] - Height[x + 1, y - 1]),
//    1.0f);
//    normal = Vector3.Normalize(normal);

//    return normal;
//}

void addVertex(uint vertex, uint x, uint y)
{
    uint scale = 5;
    uint index = getIndex(x, y);
    vec4 position = mvp * vec4(x, y, heightMap[index] * scale, 1.0);

    gl_MeshVerticesNV[vertex].gl_Position = position;
    v_out[vertex].color = vec4(1.0, 1.0, 1.0, 1.0);
}

void addQuad(uint index, uint vertex)
{
    gl_PrimitiveIndicesNV[index + 0] = vertex + 0;
    gl_PrimitiveIndicesNV[index + 1] = vertex + 1;
    gl_PrimitiveIndicesNV[index + 2] = vertex + 3;
    gl_PrimitiveIndicesNV[index + 3] = vertex + 0;
    gl_PrimitiveIndicesNV[index + 4] = vertex + 3;
    gl_PrimitiveIndicesNV[index + 5] = vertex + 2;
}

void main()
{
    uint threadNumber = gl_GlobalInvocationID.x;
    uint mapSize = uint(sqrt(heightMap.length()));
    uint meshletSize = uint(sqrt(VERTICES));
    uint yMeshletCount = uint(ceil(float(mapSize) / (meshletSize - 1)));
    uint xOffset = uint(floor(threadNumber / yMeshletCount)) * (meshletSize - 1);
    uint yOffset = (threadNumber % yMeshletCount) * (meshletSize - 1);

    if(xOffset + (meshletSize - 1) > mapSize || yOffset + (meshletSize - 1) > mapSize)
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