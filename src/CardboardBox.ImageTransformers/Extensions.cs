using ImageMagick;

namespace CardboardBox.ImageTransformers;

public static class Extensions
{
    public static async Task ConvertImage(this Stream input, Stream output, MagickFormat format = MagickFormat.Png, int quality = 75)
    {
        using var mgc = new MagickImage(input);
        mgc.Format = format;
        mgc.Quality = 75;
        await mgc.WriteAsync(output);
    }
}
