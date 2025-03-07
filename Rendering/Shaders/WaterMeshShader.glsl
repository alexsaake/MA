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

struct GridPoint
{
    float WaterHeight;
    float SuspendedSediment;
    float TempSediment;

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

layout(std430, binding = 3) readonly restrict buffer configurationShaderBuffer
{
    uint heightMultiplier;
};

uniform mat4 mvp;

uint myMapSize;

uint getIndex(uint x, uint y)
{
    return (y * myMapSize) + x;
}

void addVertex(uint vertex, uint x, uint y)
{
    uint index = getIndex(x, y);
    vec4 position = mvp * vec4(x, y, (heightMap[index] + gridPoints[index].WaterHeight) * heightMultiplier, 1.0);

    gl_MeshVerticesNV[vertex].gl_Position = position;
    v_out[vertex].position = position;
    v_out[vertex].color = vec4(0.0, 0.0, 1.0, 0.5);
    v_out[vertex].normal = vec4(0.0, 0.0, 1.0, 1.0);
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