using Raylib_cs;

namespace ProceduralLandscapeGeneration
{
    internal interface ITextureCreator
    {
        Texture2D CreateTexture(HeightMap heightMap);
    }
}