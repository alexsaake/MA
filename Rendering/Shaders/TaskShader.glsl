//https://raw.githubusercontent.com/przemyslawzaworski/CPP-Programming/master/GLSL/fireball.c

#version 460
#extension GL_NV_mesh_shader : enable

layout(local_size_x = 1) in;

void main()
{
	gl_TaskCountNV = 1;
}