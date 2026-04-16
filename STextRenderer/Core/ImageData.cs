using SkiaSharp;

namespace STextRenderer.Core;

public sealed class ImageData : IDisposable
{
    internal SKBitmap Bitmap { get; }

    private bool _disposed;

    internal ImageData(SKBitmap bitmap)
    {
        Bitmap = bitmap;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Bitmap.Dispose();
            _disposed = true;
        }
    }
}
