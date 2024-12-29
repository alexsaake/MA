#version 330

in vec3 fragPosition;
in vec3 fragNormal;
in vec4 fragColor;
in vec4 fragPosLightSpace;

uniform sampler2D shadowMap;

uniform vec3 lightDirection;
uniform vec3 viewPosition;

out vec4 TexelColor;

float ShadowCalculation()
{
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;
    float closestDepth = texture(shadowMap, projCoords.xy).r;
    float currentDepth = projCoords.z;
    vec3 normal = normalize(fragNormal);
    vec3 lightDirectionNormal = normalize(-lightDirection);
    float bias = max(0.05 * (1.0 - dot(normal, lightDirectionNormal)), 0.005);

    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    for (int x = -1; x <= 1; ++x)
    {
        for (int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(shadowMap, projCoords.xy + vec2(x, y) * texelSize).r;
            shadow += currentDepth - bias > pcfDepth ? 1.0 : 0.0;
        }
    }
    shadow /= 9.0;

    if (projCoords.z > 1.0)
        shadow = 0.0;

    return shadow;
}

void main()
{
    vec3 lightColor = vec3(1.0);

    vec3 ambient = 0.01 * lightColor;

    vec3 normal = normalize(fragNormal);
    vec3 lightDirectionNormal = normalize(-lightDirection);
    float diff = max(dot(normal, lightDirectionNormal), 0.0);
    vec3 diffuse = diff * lightColor;

    vec3 viewDirection = normalize(viewPosition - fragPosition);
    vec3 reflectDir = reflect(-lightDirectionNormal, normal);
    float spec = pow(max(dot(viewDirection, reflectDir), 0.0), 64);
    vec3 specular = spec * lightColor;

    float shadow = ShadowCalculation();

    vec3 result = (ambient + (1.0 - shadow) * (diffuse + specular)) * fragColor.rgb;
    TexelColor = vec4(result, 1.0);
}