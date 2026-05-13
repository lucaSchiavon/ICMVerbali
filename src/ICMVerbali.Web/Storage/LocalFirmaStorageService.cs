using System.Security.Cryptography;
using ICMVerbali.Web.Entities.Enums;
using Microsoft.Extensions.Options;

namespace ICMVerbali.Web.Storage;

// Implementazione filesystem-based dello storage firme. PNG arrivano gia' renderizzati
// dal signature pad client, qui scriviamo i bytes "as-is" e calcoliamo SHA-256.
// Path: "{basePath}/firme/{verbaleId}/{cse|impresa}.png". Sovrascrittura permessa.
public sealed class LocalFirmaStorageService : IFirmaStorageService
{
    private const string FirmeSubfolder = "firme";

    private readonly string _basePath;

    public LocalFirmaStorageService(IOptions<StorageOptions> options, IHostEnvironment env)
    {
        var configured = options.Value.UploadsBasePath;
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException(
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.UploadsBasePath)} non configurato.");

        _basePath = Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(env.ContentRootPath, configured));
    }

    public async Task<FirmaStorageResult> SalvaAsync(
        Guid verbaleId,
        TipoFirmatario tipo,
        byte[] pngBytes,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pngBytes);
        if (pngBytes.Length == 0)
            throw new ArgumentException("Bytes firma vuoti.", nameof(pngBytes));

        var verbaleDir = Path.Combine(_basePath, FirmeSubfolder, verbaleId.ToString("D"));
        Directory.CreateDirectory(verbaleDir);

        var fileName = $"{TipoToFileName(tipo)}.png";
        var fullPath = Path.Combine(verbaleDir, fileName);

        await File.WriteAllBytesAsync(fullPath, pngBytes, ct);

        var hash = Convert.ToHexString(SHA256.HashData(pngBytes)).ToLowerInvariant();
        var relative = Path.GetRelativePath(_basePath, fullPath).Replace('\\', '/');
        return new FirmaStorageResult(relative, pngBytes.LongLength, hash);
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

    private static string TipoToFileName(TipoFirmatario tipo) => tipo switch
    {
        TipoFirmatario.Cse => "cse",
        TipoFirmatario.ImpresaAppaltatrice => "impresa",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "TipoFirmatario non gestito."),
    };

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
