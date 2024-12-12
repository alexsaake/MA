#version 330

in vec3 vertexPosition;
in vec3 vertexNormal;

uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 lightSpaceMatrix;

out vec3 fragPosition;
out vec3 fragNormal;
out vec4 fragPosLightSpace;

void main()
{
    fragPosition = vec3(matModel * vec4(vertexPosition, 1.0));
    fragNormal = transpose(inverse(mat3(matModel))) * vertexNormal;
    fragPosLightSpace = lightSpaceMatrix * vec4(fragPosition, 1.0);
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}