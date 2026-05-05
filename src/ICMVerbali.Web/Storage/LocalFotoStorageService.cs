using Microsoft.Extensions.Options;

namespace ICMVerbali.Web.Storage;

public sealed class LocalFotoStorageService : IFotoStorageService
{
    private const string VerbaliSubfolder = "verbali";

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

    // Resize / strip-EXIF con SkiaSharp verra' aggiunto in B.9. Per ora salva il
    // contenuto cosi' com'e' arriva.
    public async Task<FotoStorageResult> SalvaAsync(
        Guid verbaleId,
        Stream contenuto,
        string nomeFileOriginale,
        CancellationToken ct = default)
    {
        var verbaleDir = Path.Combine(_basePath, VerbaliSubfolder, verbaleId.ToString("D"));
        Directory.CreateDirectory(verbaleDir);

        var estensione = Path.GetExtension(nomeFileOriginale);
        var fileName = $"{Guid.NewGuid():D}{estensione}";
        var fullPath = Path.Combine(verbaleDir, fileName);

        await using var output = new FileStream(
            fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await contenuto.CopyToAsync(output, ct);

        var relative = Path.GetRelativePath(_basePath, fullPath).Replace('\\', '/');
        return new FotoStorageResult(relative, output.Length);
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
        return Task.CompletedTask;
    }

    public Task EliminaTuttoVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
    {
        var verbaleDir = Path.Combine(_basePath, VerbaliSubfolder, verbaleId.ToString("D"));
        if (Directory.Exists(verbaleDir))
            Directory.Delete(verbaleDir, recursive: true);
        return Task.CompletedTask;
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
