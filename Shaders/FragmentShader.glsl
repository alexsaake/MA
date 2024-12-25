//https://www.geeks3d.com/hacklab/20200515/demo-rgb-triangle-with-mesh-shaders-in-opengl/

#version 450

layout(location = 0) out vec4 FragColor;

in PerVertexData
{
	vec4 position;
	vec4 color;
	vec3 normal;
} fragIn;

uniform vec3 lightDirection;
uniform vec3 viewPosition;

void main()
{
    vec3 lightColor = vec3(1.0);

	vec3 ambient = 0.01 * lightColor;

	vec3 normal = normalize(fragIn.normal);
    vec3 lightDirectionNormal = normalize(-lightDirection);  
    float diff = max(dot(normal, lightDirectionNormal), 0.0);
    vec3 diffuse = diff * lightColor;
    
    vec3 viewDirection = normalize(viewPosition - fragIn.position.xyz);
    vec3 reflectDir = reflect(-lightDirectionNormal, normal);
    float spec = pow(max(dot(viewDirection, reflectDir), 0.0), 64);
    vec3 specular = spec * lightColor;

    vec3 result =  (ambient + diffuse + specular) * fragIn.color.rgb;
    FragColor = vec4(result, 1.0);
}