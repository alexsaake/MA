using Microsoft.Toolkit.HighPerformance;
using Raylib_CsLo;
using Raylib_CsLo.InternalHelpers;
using System.Runtime.CompilerServices;

namespace ProceduralLandscapeGeneration
{
    internal class MapDisplay : IMapDisplay
    {
        private IMapGenerator myMapGenerator;

        public MapDisplay(IMapGenerator mapGenerator)
        {
            myMapGenerator = mapGenerator;
        }

        public unsafe Texture CreateNoiseTexture(int width, int height)
        {
            float[,] noiseMap = myMapGenerator.GenerateNoiseMap(width, height);

            // Dynamic memory allocation to store pixels data (Color type)
            //Color* pixels = (Color*)malloc(width * height * sizeof(Color));
            //var pixels = stackalloc Color[width * height];
            var pixels = new Color[width * height];
            var h_pixels = pixels.GcPin();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte color = (byte)((noiseMap[x, y] + 1) * 0.5 * 255);
                    pixels[y * width + x] = new Color(color, color, color, byte.MaxValue);
                }
            }

            // Load pixels data into an image structure and create texture
            Image checkedIm = new()
            {
                data = Unsafe.AsPointer(ref pixels.DangerousGetReference()),             // We can assign pixels directly to data
                width = width,
                height = height,
                format = (int)PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8,
                mipmaps = 1
            };
            Texture texture = Raylib.LoadTextureFromImage(checkedIm);
            //RAYLIB-CSLO: our pixels are created in managed memory.  will be cleaned up by the GC
            //UnloadImage(checkedIm);         // Unload CPU (RAM) image data (pixels)
            //---------------------------------------------------------------------------------------

            return texture;
        }
    }
}
