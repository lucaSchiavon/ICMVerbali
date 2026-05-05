namespace ICMVerbali.Web.Storage;

// Astrazione dello storage immagini. L'implementazione locale vive su filesystem;
// in futuro potra' essere sostituita da Azure Blob / S3 senza toccare i Manager.
// Vedi docs/01-design.md §7.
public interface IFotoStorageService
{
    Task<FotoStorageResult> SalvaAsync(
        Guid verbaleId,
        Stream contenuto,
        string nomeFileOriginale,
        CancellationToken ct = default);

    Task<Stream> ApriLetturaAsync(string filePathRelativo, CancellationToken ct = default);

    Task EliminaAsync(string filePathRelativo, CancellationToken ct = default);

    Task EliminaTuttoVerbaleAsync(Guid verbaleId, CancellationToken ct = default);
}

public sealed record FotoStorageResult(string FilePathRelativo, long Bytes);
