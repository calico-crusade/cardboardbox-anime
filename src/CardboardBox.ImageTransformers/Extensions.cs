using ImageMagick;

namespace CardboardBox.ImageTransformers;

public static class Extensions
{
    public static async Task ConvertImage(this Stream input, Stream output, MagickFormat format = MagickFormat.Png, int quality = 75, int maxSize = 1024)
    {
        using var mgc = new MagickImage(input);

        if (mgc.Width > maxSize || mgc.Height > maxSize)
        {
            var ratio = new[] 
            { 
                (double)maxSize / mgc.Width, 
                (double)maxSize / mgc.Height 
            }.Min();

            mgc.Resize(new Percentage(ratio * 100));
        }
        
        mgc.Format = format;
        mgc.Quality = quality;
        await mgc.WriteAsync(output);
    }
}
