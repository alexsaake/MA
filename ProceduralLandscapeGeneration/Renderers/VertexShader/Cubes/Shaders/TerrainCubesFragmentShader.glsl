#version 330

in vec3 fragPosition;
in vec4 fragColor;
in vec3 fragNormal;

uniform vec3 lightDirection;
uniform vec3 viewPosition;

out vec4 TexelColor;

void main()
{
    vec3 lightColor = vec3(1.0);

    vec3 ambient = 0.2 * lightColor;

    vec3 normal = normalize(fragNormal);
    vec3 lightDirectionNormal = normalize(-lightDirection);
    float diff = max(dot(normal, lightDirectionNormal), 0.0);
    vec3 diffuse = diff * lightColor;

    vec3 viewDirection = normalize(viewPosition - fragPosition);
    vec3 reflectDir = reflect(-lightDirectionNormal, normal);
    float spec = pow(max(dot(viewDirection, reflectDir), 0.0), 2);
    vec3 specular = spec * lightColor * 0.2;

    vec3 result = (ambient + 1.0 * (diffuse + specular)) * fragColor.rgb;
    TexelColor = vec4(result, fragColor.a);
}