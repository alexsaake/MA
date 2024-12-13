using Raylib_CsLo;

namespace ProceduralLandscapeGeneration
{
    internal interface ITextureCreator
    {
        Texture CreateTexture(HeightMap heightMap);
    }
}