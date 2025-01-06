#version 430

//https://www.shadertoy.com/view/NlSGDz
// implementation of MurmurHash (https://sites.google.com/site/murmurhash/) for a 
// single unsigned integer.

uint hash(uint x, uint seed) {
    const uint m = 0x5bd1e995U;
    uint hash = seed;
    // process input
    uint k = x;
    k *= m;
    k ^= k >> 24;
    k *= m;
    hash *= m;
    hash ^= k;
    // some final mixing
    hash ^= hash >> 13;
    hash *= m;
    hash ^= hash >> 15;
    return hash;
}

// implementation of MurmurHash (https://sites.google.com/site/murmurhash/) for a  
// 2-dimensional unsigned integer input vector.

uint hash(uvec2 x, uint seed){
    const uint m = 0x5bd1e995U;
    uint hash = seed;
    // process first vector element
    uint k = x.x; 
    k *= m;
    k ^= k >> 24;
    k *= m;
    hash *= m;
    hash ^= k;
    // process second vector element
    k = x.y; 
    k *= m;
    k ^= k >> 24;
    k *= m;
    hash *= m;
    hash ^= k;
	// some final mixing
    hash ^= hash >> 13;
    hash *= m;
    hash ^= hash >> 15;
    return hash;
}


vec2 gradientDirection(uint hash) {
    switch (int(hash) & 3) { // look at the last two bits to pick a gradient direction
    case 0:
        return vec2(1.0, 1.0);
    case 1:
        return vec2(-1.0, 1.0);
    case 2:
        return vec2(1.0, -1.0);
    case 3:
        return vec2(-1.0, -1.0);
    }
}

float interpolate(float value1, float value2, float value3, float value4, vec2 t) {
    return mix(mix(value1, value2, t.x), mix(value3, value4, t.x), t.y);
}

vec2 fade(vec2 t) {
    // 6t^5 - 15t^4 + 10t^3
	return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

float perlinNoise(vec2 position, uint seed) {
    vec2 floorPosition = floor(position);
    vec2 fractPosition = position - floorPosition;
    uvec2 cellCoordinates = uvec2(floorPosition);
    float value1 = dot(gradientDirection(hash(cellCoordinates, seed)), fractPosition);
    float value2 = dot(gradientDirection(hash((cellCoordinates + uvec2(1, 0)), seed)), fractPosition - vec2(1.0, 0.0));
    float value3 = dot(gradientDirection(hash((cellCoordinates + uvec2(0, 1)), seed)), fractPosition - vec2(0.0, 1.0));
    float value4 = dot(gradientDirection(hash((cellCoordinates + uvec2(1, 1)), seed)), fractPosition - vec2(1.0, 1.0));
    return interpolate(value1, value2, value3, value4, fade(fractPosition));
}

// original author Sebastian Lague
// https://github.com/SebLague/Hydraulic-Erosion/blob/master/Assets/Scripts/ComputeShaders/HeightMap.compute﻿

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

struct HeightMapParameters
{
    uint seed;
    uint sideLength;
    float scale;
    uint octaves;
    float persistence;
    float lacunarity;
    int min;
    int max;
};

layout(std430, binding = 1) buffer heightMapParametersShaderBuffer
{
    HeightMapParameters parameters;
};

layout(std430, binding = 2) buffer heightMapShaderBuffer
{
    float[] heightMap;
};

void main()
{
    uint id = gl_GlobalInvocationID.x;
    
    uint mapSize = parameters.sideLength * parameters.sideLength;
    if (id >= mapSize) return;
    
    uint x = id % parameters.sideLength;
    uint y = id / parameters.sideLength;
    
    float amplitude = 1;
    float frequency = 1;
    float noiseHeight = 0;
    
    uint currentSeed = uint(parameters.seed);
    for (int octave = 0; octave < parameters.octaves; octave++)
    {
        currentSeed = hash(currentSeed, 0x0U); // create a new seed for each octave
        float sampleX = x / (parameters.scale * 100) * frequency;
        float sampleY = y / (parameters.scale * 100) * frequency;

        float perlinValue = perlinNoise(vec2(sampleX, sampleY), currentSeed);
        noiseHeight += perlinValue * amplitude;

        amplitude *= parameters.persistence;
        frequency *= parameters.lacunarity;
    }

    heightMap[id] = noiseHeight;
    int val = int(noiseHeight * 100000);
    atomicMin(parameters.min, val);
    atomicMax(parameters.max, val);
}