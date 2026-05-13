using ICMVerbali.Web.Entities.Enums;

namespace ICMVerbali.Web.Storage;

// Storage delle immagini di firma (PNG generati dal signature pad lato client).
// Convenzione path: "firme/{verbaleId}/{cse|impresa}.png". Una sola firma per
// (verbale, tipo) — stesso vincolo UNIQUE del DB. SalvaAsync sovrascrive se la
// firma esiste gia' (ri-firma in caso di errore correggibile, vedi §9.8).
// Hash SHA-256 calcolato e ritornato per riga di audit / verifica integrita'.
public interface IFirmaStorageService
{
    Task<FirmaStorageResult> SalvaAsync(
        Guid verbaleId,
        TipoFirmatario tipo,
        byte[] pngBytes,
        CancellationToken ct = default);

    Task<Stream> ApriLetturaAsync(string filePathRelativo, CancellationToken ct = default);

    Task EliminaAsync(string filePathRelativo, CancellationToken ct = default);
}

public sealed record FirmaStorageResult(string FilePathRelativo, long Bytes, string Sha256Hex);
