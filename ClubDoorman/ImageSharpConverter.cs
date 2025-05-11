using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace ClubDoorman;

public static class ImageSharpConverter
{
    public static float[][,] ConvertToNormalizedBgrFloatChannels(Stream imageStream)
    {
        using Image<Rgb24> image = Image.Load<Rgb24>(imageStream);

        int width = image.Width;
        int height = image.Height;

        float[,] b = new float[height, width];
        float[,] g = new float[height, width];
        float[,] r = new float[height, width];

        const float inv255 = 1f / 255f;

        for (int y = 0; y < height; y++)
        {
            var rowSpan = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < width; x++)
            {
                var pixel = rowSpan[x];
                b[y, x] = pixel.B * inv255;
                g[y, x] = pixel.G * inv255;
                r[y, x] = pixel.R * inv255;
            }
        }

        return [b, g, r];
    }
}
