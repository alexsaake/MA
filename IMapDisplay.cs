using Raylib_CsLo;

namespace ProceduralLandscapeGeneration
{
    internal interface IMapDisplay
    {
        Texture CreateNoiseTexture(float[,] noiseMap);
    }
}