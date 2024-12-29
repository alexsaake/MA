#version 330

in vec3 vertexPosition;
in vec3 vertexNormal;
in vec4 vertexColor;

uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 lightSpaceMatrix;

out vec3 fragPosition;
out vec3 fragNormal;
out vec4 fragColor;
out vec4 fragPosLightSpace;

void main()
{
    fragPosition = vec3(matModel * vec4(vertexPosition, 1.0));
    fragNormal = transpose(inverse(mat3(matModel))) * vertexNormal;
    fragColor = vertexColor;
    fragPosLightSpace = lightSpaceMatrix * vec4(fragPosition, 1.0);
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}