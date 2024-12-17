using Microsoft.Toolkit.HighPerformance;
using Raylib_CsLo;
using Raylib_CsLo.InternalHelpers;
using System.Runtime.CompilerServices;

namespace ProceduralLandscapeGeneration
{
    internal class TextureCreator : ITextureCreator
    {
        public unsafe Texture CreateTexture(HeightMap heightMap)
        {
            // Dynamic memory allocation to store pixels data (Color type)
            //Color* pixels = (Color*)malloc(width * height * sizeof(Color));
            //var pixels = stackalloc Color[width * height];
            var pixels = new Color[heightMap.Width * heightMap.Depth];
            var h_pixels = pixels.GcPin();
            for (int y = 0; y < heightMap.Depth; y++)
            {
                for (int x = 0; x < heightMap.Width; x++)
                {
                    byte color = (byte)((heightMap.Value[x, y].Height + 1) * 0.5 * 255);
                    pixels[y * heightMap.Width + x] = new Color(color, color, color, byte.MaxValue);
                }
            }

            Image image = new()
            {
                data = Unsafe.AsPointer(ref pixels.DangerousGetReference()),
                width = heightMap.Width,
                height = heightMap.Depth,
                format = (int)PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8,
                mipmaps = 1
            };
            Texture texture = Raylib.LoadTextureFromImage(image);

            return texture;
        }
    }
}
