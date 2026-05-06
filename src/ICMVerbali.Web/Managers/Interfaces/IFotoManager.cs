using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Managers.Interfaces;

// Manager per le foto del verbale (sez. 9 PDF). Orchestra storage filesystem +
// repository: validazione formato/peso, salvataggio file (con resize + thumb
// via SkiaSharp), insert metadati. La UI passa solo il payload, mai paths o
// dettagli dell'IFotoStorageService.
public interface IFotoManager
{
    Task<IReadOnlyList<Foto>> GetByVerbaleAsync(Guid verbaleId, CancellationToken ct = default);

    // Upload di una nuova foto. fileName e' il nome originale (server lo usa per
    // dedurre l'estensione e validare il content-type). contentLength e' il peso
    // dichiarato dal browser, validato contro MaxFileSizeBytes prima di leggere
    // il payload (il caller deve gia' aver controllato anche contentType).
    // Restituisce la Foto creata con Id/Ordine valorizzati.
    Task<Foto> UploadAsync(
        Guid verbaleId,
        Stream contenuto,
        string fileName,
        long contentLength,
        CancellationToken ct = default);

    Task UpdateDidascaliaAsync(Guid id, string? didascalia, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // Riordina la lista foto del verbale: prende l'array degli Id nell'ordine
    // desiderato dalla UI, il manager rinumera Ordine 1..N.
    Task ReorderAsync(Guid verbaleId, IReadOnlyList<Guid> idsInOrder, CancellationToken ct = default);

    // Cap dimensione singola foto pre-elaborazione (15 MB). Dopo il resize a
    // 1920px le foto smartphone tipiche scendono sotto i 2 MB.
    public const long MaxFileSizeBytes = 15L * 1024 * 1024;

    // Estensioni accettate (case-insensitive, con punto). Il check sui magic
    // bytes lo fa SkiaSharp che rifiuta i formati non supportati al decode.
    public static readonly IReadOnlyCollection<string> AllowedExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif" };
}
