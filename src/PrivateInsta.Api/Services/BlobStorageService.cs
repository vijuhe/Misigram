using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SkiaSharp;

namespace PrivateInsta.Api.Services;

public class BlobStorageService(IConfiguration config)
{
    private readonly BlobContainerClient _container = new(
        config["BlobStorage:Connection"],
        config["BlobStorage:ContainerName"]);

    public async Task<string> UploadAsync(Stream stream, string blobPath, string contentType, CancellationToken ct = default)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        var blob = _container.GetBlobClient(blobPath);
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
        return blobPath;
    }

    public async Task<string> UploadWithThumbnailAsync(Stream imageStream, string basePath, string ext, string contentType, CancellationToken ct = default)
    {
        imageStream.Position = 0;
        await UploadAsync(imageStream, $"{basePath}/original{ext}", contentType, ct);

        imageStream.Position = 0;
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        // Read EXIF orientation before decoding so we can bake it into the thumbnail
        var origin = SKEncodedOrigin.TopLeft;
        using (var skData = SKData.CreateCopy(bytes))
        using (var codec = SKCodec.Create(skData))
            if (codec is not null) origin = codec.EncodedOrigin;

        using var bitmap = SKBitmap.Decode(bytes);
        using var oriented = ApplyExifOrientation(bitmap, origin);

        var scale = Math.Min(600f / oriented.Width, 600f / oriented.Height);
        int w = Math.Min((int)(oriented.Width * scale), 600);
        int h = Math.Min((int)(oriented.Height * scale), 600);
        using var resized = oriented.Resize(new SKImageInfo(w, h), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        using var img = SKImage.FromBitmap(resized);
        using var thumbBytes = img.Encode(SKEncodedImageFormat.Jpeg, 85);

        using var thumbStream = thumbBytes.AsStream();
        return await UploadAsync(thumbStream, $"{basePath}/thumb.jpg", "image/jpeg", ct);
    }

    public async Task DeleteAsync(string blobPath, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobPath);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }

    public string GenerateSasUrl(string blobPath, int expiryMinutes = 60)
    {
        var blob = _container.GetBlobClient(blobPath);
        var sas = blob.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(expiryMinutes));
        return sas.ToString();
    }

    // Returns a fresh SAS URL for a stored blob path, or passes through external URLs (e.g. Google profile pictures) unchanged.
    // Also handles legacy rows where a full SAS URL was stored instead of a blob path.
    public string? ResolveSasUrl(string? blobPathOrExternalUrl, int expiryMinutes = 60)
    {
        if (string.IsNullOrEmpty(blobPathOrExternalUrl)) return blobPathOrExternalUrl;

        if (blobPathOrExternalUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            // Legacy SAS URL stored in DB: extract blob path and regenerate
            if (blobPathOrExternalUrl.Contains(".blob.core.windows.net/", StringComparison.OrdinalIgnoreCase))
            {
                var marker = $"/{_container.Name}/";
                var idx = blobPathOrExternalUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var afterContainer = blobPathOrExternalUrl[(idx + marker.Length)..];
                    var blobPath = afterContainer.Contains('?') ? afterContainer[..afterContainer.IndexOf('?')] : afterContainer;
                    return GenerateSasUrl(blobPath, expiryMinutes);
                }
            }
            return blobPathOrExternalUrl; // External URL (e.g. Google profile picture)
        }

        return GenerateSasUrl(blobPathOrExternalUrl, expiryMinutes);
    }

    // Rotates/flips a decoded bitmap to match the EXIF orientation so thumbnails
    // display correctly without relying on browser EXIF support.
    private static SKBitmap ApplyExifOrientation(SKBitmap src, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft) return src;

        int W = src.Width, H = src.Height;
        bool swapDims = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
                                or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        int outW = swapDims ? H : W;
        int outH = swapDims ? W : H;

        // Build the affine matrix that maps source pixel (x,y) to its correct output position.
        SKMatrix matrix = origin switch
        {
            SKEncodedOrigin.TopRight    => new SKMatrix { ScaleX = -1, SkewX =  0, TransX = W - 1,
                                                          SkewY  =  0, ScaleY =  1, TransY = 0,     Persp2 = 1 }, // flip H
            SKEncodedOrigin.BottomRight => new SKMatrix { ScaleX = -1, SkewX =  0, TransX = W - 1,
                                                          SkewY  =  0, ScaleY = -1, TransY = H - 1, Persp2 = 1 }, // 180°
            SKEncodedOrigin.BottomLeft  => new SKMatrix { ScaleX =  1, SkewX =  0, TransX = 0,
                                                          SkewY  =  0, ScaleY = -1, TransY = H - 1, Persp2 = 1 }, // flip V
            SKEncodedOrigin.LeftTop     => new SKMatrix { ScaleX =  0, SkewX =  1, TransX = 0,
                                                          SkewY  =  1, ScaleY =  0, TransY = 0,     Persp2 = 1 }, // transpose
            SKEncodedOrigin.RightTop    => new SKMatrix { ScaleX =  0, SkewX = -1, TransX = H - 1,
                                                          SkewY  =  1, ScaleY =  0, TransY = 0,     Persp2 = 1 }, // 90° CW
            SKEncodedOrigin.RightBottom => new SKMatrix { ScaleX =  0, SkewX = -1, TransX = H - 1,
                                                          SkewY  = -1, ScaleY =  0, TransY = W - 1, Persp2 = 1 }, // anti-transpose
            SKEncodedOrigin.LeftBottom  => new SKMatrix { ScaleX =  0, SkewX =  1, TransX = 0,
                                                          SkewY  = -1, ScaleY =  0, TransY = W - 1, Persp2 = 1 }, // 90° CCW
            _ => SKMatrix.Identity
        };

        var result = new SKBitmap(outW, outH);
        using var canvas = new SKCanvas(result);
        canvas.Clear();
        canvas.SetMatrix(matrix);
        canvas.DrawBitmap(src, 0, 0);
        return result;
    }
}
