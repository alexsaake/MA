#version 330

in vec3 vertexPosition;
in vec3 vertexNormal;

uniform mat4 matModel;
uniform mat4 mvp;

out vec3 fragColor;

void main()
{
	vec3 lightPosition = vec3(0.2, 0.6, 0.3);
	float diffuse = clamp(dot(normalize(vertexNormal), normalize(lightPosition)), 0.1,  0.9);
	float ambient = 0.01;

	float steepness = 0.97;
	vec3 lightColor = vec3(1.0, 1.0, 1.0);
	float lightStrength = 2.0;
	vec3 light = lightColor * lightStrength * (diffuse + ambient);
	if(normalize(vertexNormal).z < steepness)
	{
		vec3 steepColor = vec3(0.7, 0.7, 0.7);
		fragColor = steepColor * light;
	}
	else
	{
		vec3 flatColor = vec3(0.3, 0.6, 0.3);
		fragColor = flatColor * light;
	}

    gl_Position = mvp * vec4(vertexPosition, 1.0);
}