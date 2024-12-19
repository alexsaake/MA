using Microsoft.Toolkit.HighPerformance;
using Raylib_cs;
using System.Runtime.CompilerServices;

namespace ProceduralLandscapeGeneration
{
    internal class TextureCreator : ITextureCreator
    {
        public unsafe Texture2D CreateTexture(HeightMap heightMap)
        {
            // Dynamic memory allocation to store pixels data (Color type)
            //Color* pixels = (Color*)malloc(width * height * sizeof(Color));
            //var pixels = stackalloc Color[width * height];
            var pixels = new Color[heightMap.Width * heightMap.Depth];
            for (int y = 0; y < heightMap.Depth; y++)
            {
                for (int x = 0; x < heightMap.Width; x++)
                {
                    byte color = (byte)((heightMap.Height[x, y] + 1) * 0.5 * 255);
                    pixels[y * heightMap.Width + x] = new Color(color, color, color, byte.MaxValue);
                }
            }

            Image image = new()
            {
                Data = Unsafe.AsPointer(ref pixels.DangerousGetReference()),
                Width = heightMap.Width,
                Height = heightMap.Depth,
                Format = PixelFormat.UncompressedR8G8B8A8,
                Mipmaps = 1
            };
            Texture2D texture = Raylib.LoadTextureFromImage(image);

            return texture;
        }
    }
}
