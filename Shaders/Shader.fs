#version 330

in vec3 fragColor;

out vec4 texelColor;

void main()
{
    texelColor = vec4(fragColor, 1.0);
}