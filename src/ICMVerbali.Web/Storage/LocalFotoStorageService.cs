using Microsoft.Extensions.Options;
using SkiaSharp;

namespace ICMVerbali.Web.Storage;

// Filesystem-backed storage per le foto dei verbali.
// Pipeline su SalvaAsync (B.9):
//   1. Decode con SKCodec (preserva orientamento EXIF)
//   2. Auto-orient: il bitmap diventa "diritto" come l'utente vede la foto sullo smartphone
//   3. Resize lato lungo a 1920px se piu' grande (preserva ratio)
//   4. Encode JPEG q85 -> file full-size
//   5. Thumbnail 200x200 cover-crop (square center crop), encode JPEG q75 -> file .thumb.jpg
// Strip EXIF di default (re-encode pulito).
public sealed class LocalFotoStorageService : IFotoStorageService
{
    private const string VerbaliSubfolder = "verbali";
    private const int FullMaxSide = 1920;
    private const int ThumbSize = 200;
    private const int FullJpegQuality = 85;
    private const int ThumbJpegQuality = 75;

    private readonly string _basePath;

    public LocalFotoStorageService(IOptions<StorageOptions> options, IHostEnvironment env)
    {
        var configured = options.Value.UploadsBasePath;
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException(
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.UploadsBasePath)} non configurato.");

        _basePath = Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(env.ContentRootPath, configured));
    }

    public async Task<FotoStorageResult> SalvaAsync(
        Guid verbaleId,
        Stream contenuto,
        string nomeFileOriginale,
        CancellationToken ct = default)
    {
        // SKCodec.Create vuole uno stream seekable. Se non lo e', copiamo in MemoryStream.
        Stream sourceStream = contenuto;
        MemoryStream? buffered = null;
        if (!contenuto.CanSeek)
        {
            buffered = new MemoryStream();
            await contenuto.CopyToAsync(buffered, ct);
            buffered.Position = 0;
            sourceStream = buffered;
        }

        try
        {
            using var skStream = new SKManagedStream(sourceStream);
            using var codec = SKCodec.Create(skStream)
                ?? throw new InvalidOperationException("Formato immagine non supportato.");

            // Decode in raw pixels.
            var info = codec.Info;
            using var raw = new SKBitmap(info.Width, info.Height, info.ColorType, info.AlphaType);
            var result = codec.GetPixels(raw.Info, raw.GetPixels());
            if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
                throw new InvalidOperationException($"Decodifica immagine fallita: {result}.");

            // Auto-orient secondo EXIF.
            using var oriented = ApplyOrientation(raw, codec.EncodedOrigin);

            // Resize full-size se necessario.
            using var resized = ResizeIfTooLarge(oriented, FullMaxSide);

            // Save full + thumb.
            var verbaleDir = Path.Combine(_basePath, VerbaliSubfolder, verbaleId.ToString("D"));
            Directory.CreateDirectory(verbaleDir);

            var fileBaseName = Guid.CreateVersion7().ToString("D");
            var fullPath = Path.Combine(verbaleDir, $"{fileBaseName}.jpg");
            var thumbPath = Path.Combine(verbaleDir, $"{fileBaseName}.thumb.jpg");

            long fullBytes;
            await using (var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using var image = SKImage.FromBitmap(resized);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, FullJpegQuality);
                data.SaveTo(output);
                fullBytes = output.Length;
            }

            using (var thumb = CreateThumbnail(resized, ThumbSize))
            await using (var output = new FileStream(thumbPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using var image = SKImage.FromBitmap(thumb);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, ThumbJpegQuality);
                data.SaveTo(output);
            }

            var relative = Path.GetRelativePath(_basePath, fullPath).Replace('\\', '/');
            return new FotoStorageResult(relative, fullBytes);
        }
        finally
        {
            buffered?.Dispose();
        }
    }

    public Task<Stream> ApriLetturaAsync(string filePathRelativo, CancellationToken ct = default)
    {
        var fullPath = ResolveAndValidate(filePathRelativo);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task EliminaAsync(string filePathRelativo, CancellationToken ct = default)
    {
        var fullPath = ResolveAndValidate(filePathRelativo);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        // Elimina anche il thumb associato (best-effort: se non esiste, ignora).
        var thumbRelativo = GetThumbPathRelativo(filePathRelativo);
        var thumbAbsoluto = ResolveAndValidate(thumbRelativo);
        if (File.Exists(thumbAbsoluto))
            File.Delete(thumbAbsoluto);

        return Task.CompletedTask;
    }

    public Task EliminaTuttoVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
    {
        var verbaleDir = Path.Combine(_basePath, VerbaliSubfolder, verbaleId.ToString("D"));
        if (Directory.Exists(verbaleDir))
            Directory.Delete(verbaleDir, recursive: true);
        return Task.CompletedTask;
    }

    public string GetThumbPathRelativo(string filePathRelativo)
    {
        // "verbali/{id}/abc.jpg" -> "verbali/{id}/abc.thumb.jpg"
        var dir = Path.GetDirectoryName(filePathRelativo)?.Replace('\\', '/') ?? string.Empty;
        var nameNoExt = Path.GetFileNameWithoutExtension(filePathRelativo);
        var ext = Path.GetExtension(filePathRelativo);
        var thumbName = $"{nameNoExt}.thumb{ext}";
        return string.IsNullOrEmpty(dir) ? thumbName : $"{dir}/{thumbName}";
    }

    // ---------- helpers ----------------------------------------------------

    private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        // Per le foto smartphone gli origin tipici sono TopLeft (no-op) o RightTop
        // (rotazione 90 per scatti in portrait). Gestiamo tutti gli 8 casi per
        // robustezza, ma il path comune e' una semplice copia o rotazione.
        switch (origin)
        {
            case SKEncodedOrigin.TopLeft:
                return source.Copy();

            case SKEncodedOrigin.TopRight: // flip H
                return TransformBitmap(source, source.Width, source.Height, canvas =>
                {
                    canvas.Scale(-1, 1, source.Width / 2f, 0);
                });

            case SKEncodedOrigin.BottomRight: // 180
                return TransformBitmap(source, source.Width, source.Height, canvas =>
                {
                    canvas.RotateDegrees(180, source.Width / 2f, source.Height / 2f);
                });

            case SKEncodedOrigin.BottomLeft: // flip V
                return TransformBitmap(source, source.Width, source.Height, canvas =>
                {
                    canvas.Scale(1, -1, 0, source.Height / 2f);
                });

            case SKEncodedOrigin.LeftTop: // rot 90 + flip H
                return TransformBitmap(source, source.Height, source.Width, canvas =>
                {
                    canvas.Scale(-1, 1, source.Height / 2f, 0);
                    canvas.RotateDegrees(90);
                    canvas.Translate(0, -source.Height);
                });

            case SKEncodedOrigin.RightTop: // rot 90
                return TransformBitmap(source, source.Height, source.Width, canvas =>
                {
                    canvas.Translate(source.Height, 0);
                    canvas.RotateDegrees(90);
                });

            case SKEncodedOrigin.RightBottom: // rot 270 + flip H
                return TransformBitmap(source, source.Height, source.Width, canvas =>
                {
                    canvas.Scale(-1, 1, source.Height / 2f, 0);
                    canvas.Translate(0, source.Width);
                    canvas.RotateDegrees(270);
                });

            case SKEncodedOrigin.LeftBottom: // rot 270
                return TransformBitmap(source, source.Height, source.Width, canvas =>
                {
                    canvas.Translate(0, source.Width);
                    canvas.RotateDegrees(270);
                });

            default:
                return source.Copy();
        }
    }

    private static SKBitmap TransformBitmap(SKBitmap source, int newW, int newH, Action<SKCanvas> applyTransform)
    {
        var dst = new SKBitmap(newW, newH);
        using var canvas = new SKCanvas(dst);
        applyTransform(canvas);
        canvas.DrawBitmap(source, 0, 0);
        return dst;
    }

    private static SKBitmap ResizeIfTooLarge(SKBitmap source, int maxSide)
    {
        var w = source.Width;
        var h = source.Height;
        if (w <= maxSide && h <= maxSide)
            return source.Copy();

        double scale = (w >= h)
            ? (double)maxSide / w
            : (double)maxSide / h;
        var newW = (int)Math.Round(w * scale);
        var newH = (int)Math.Round(h * scale);

        var info = new SKImageInfo(newW, newH);
        var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
        return source.Resize(info, samplingOptions)
            ?? throw new InvalidOperationException("Resize fallito.");
    }

    private static SKBitmap CreateThumbnail(SKBitmap source, int size)
    {
        // Cover-crop: scala in modo che il lato minore = size, poi center-crop a size x size.
        var srcW = source.Width;
        var srcH = source.Height;

        double scale = (srcW < srcH)
            ? (double)size / srcW
            : (double)size / srcH;
        var scaledW = (int)Math.Round(srcW * scale);
        var scaledH = (int)Math.Round(srcH * scale);

        var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
        using var scaled = source.Resize(new SKImageInfo(scaledW, scaledH), samplingOptions)
            ?? throw new InvalidOperationException("Resize thumbnail fallito.");

        var offsetX = (scaledW - size) / 2;
        var offsetY = (scaledH - size) / 2;

        var thumb = new SKBitmap(size, size);
        using var canvas = new SKCanvas(thumb);
        canvas.DrawBitmap(scaled,
            source: new SKRect(offsetX, offsetY, offsetX + size, offsetY + size),
            dest: new SKRect(0, 0, size, size));
        return thumb;
    }

    // Difesa contro path traversal: il path risolto deve restare sotto _basePath.
    private string ResolveAndValidate(string filePathRelativo)
    {
        if (Path.IsPathRooted(filePathRelativo))
            throw new InvalidOperationException("Atteso path relativo.");

        var fullPath = Path.GetFullPath(Path.Combine(_basePath, filePathRelativo));
        var normalizedBase = _basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Path traversal rilevato.");

        return fullPath;
    }
}
