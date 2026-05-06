namespace ICMVerbali.Web.Storage;

// Astrazione dello storage immagini. L'implementazione locale vive su filesystem;
// in futuro potra' essere sostituita da Azure Blob / S3 senza toccare i Manager.
// Vedi docs/01-design.md §7. SalvaAsync genera DUE file: full-size (resize lato
// lungo 1920px) + thumbnail (200x200, cover crop). Il thumb path e' derivabile
// dal full path tramite GetThumbPathRelativo.
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

    // Convenzione: thumb sta accanto al full con suffisso ".thumb" prima dell'estensione.
    // Esposto per permettere agli endpoint /thumb di derivare il path senza salvarlo nel DB.
    string GetThumbPathRelativo(string filePathRelativo);
}

public sealed record FotoStorageResult(string FilePathRelativo, long Bytes);
