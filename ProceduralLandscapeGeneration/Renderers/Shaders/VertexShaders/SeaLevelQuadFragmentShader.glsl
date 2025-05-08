#version 330

in vec4 fragColor;

out vec4 TexelColor;

void main()
{
    TexelColor = fragColor;
}