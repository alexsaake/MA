//https://www.geeks3d.com/hacklab/20200515/demo-rgb-triangle-with-mesh-shaders-in-opengl/

#version 450

layout(location = 0) out vec4 FragColor;

in PerVertexData
{
	vec4 color;
} fragIn;

void main()
{
	FragColor = fragIn.color;
}