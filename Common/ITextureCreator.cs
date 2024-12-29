using Raylib_cs;

namespace ProceduralLandscapeGeneration.Common
{
    internal interface ITextureCreator
    {
        Texture2D CreateTexture(HeightMap heightMap);
    }
}