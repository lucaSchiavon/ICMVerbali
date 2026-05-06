using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Repositories.Interfaces;

// Repository per le foto del verbale (sez. 9 PDF). Persiste solo i metadati:
// il blob vive sul filesystem via IFotoStorageService. Tutte le scritture
// fanno bump UpdatedAt sul Verbale (vedi pattern bulk update B.8c).
public interface IFotoRepository
{
    Task<IReadOnlyList<Foto>> GetByVerbaleAsync(Guid verbaleId, CancellationToken ct = default);

    Task<Foto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Inserisce nuova foto. CreatedAt e' lasciato al DEFAULT del DB. Ordine viene
    // calcolato come max(Ordine) + 1 nella stessa transazione per concorrenza-safe.
    Task CreateAsync(Foto foto, CancellationToken ct = default);

    Task UpdateDidascaliaAsync(Guid id, string? didascalia, CancellationToken ct = default);

    // Restituisce il FilePathRelativo dell'eventuale riga eliminata (null se non
    // trovata): serve al manager per pulire il filesystem in caso di success.
    Task<string?> DeleteAsync(Guid id, CancellationToken ct = default);

    // Aggiorna Ordine in bulk: prende una lista di (Id -> nuovo Ordine).
    // Una transazione, UPDATE multipla via Dapper IEnumerable + bump UpdatedAt.
    Task UpdateOrdineBulkAsync(
        Guid verbaleId,
        IEnumerable<FotoOrdineUpdate> updates,
        CancellationToken ct = default);
}

public sealed record FotoOrdineUpdate(Guid Id, int Ordine);
