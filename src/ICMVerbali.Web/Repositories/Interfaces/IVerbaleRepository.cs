using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Models;

namespace ICMVerbali.Web.Repositories.Interfaces;

// Repository dell'aggregate root Verbale. Le scritture passano per
// CreateBozzaWithChildrenAsync che inserisce in transazione il record principale,
// le figlie checklist pre-popolate e la riga di audit "Creazione" (§9.12).
// Le letture per la Home usano DTO joinati (VerbaleListItem) per evitare N+1.
public interface IVerbaleRepository
{
    // Crea bozza completa: Verbale + N righe in VerbaleAttivita / VerbaleDocumento /
    // VerbaleApprestamento / VerbaleCondizioneAmbientale + 1 riga in VerbaleAudit.
    // Tutto in una transazione: in caso di errore nessuna riga residua.
    Task CreateBozzaWithChildrenAsync(
        Verbale verbale,
        IEnumerable<VerbaleAttivita> attivita,
        IEnumerable<VerbaleDocumento> documenti,
        IEnumerable<VerbaleApprestamento> apprestamenti,
        IEnumerable<VerbaleCondizioneAmbientale> condizioniAmbientali,
        VerbaleAudit audit,
        CancellationToken ct = default);

    Task<Verbale?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // Verbali del giorno (esclude bozze e soft-deleted), ordinati per UpdatedAt DESC.
    Task<IReadOnlyList<VerbaleListItem>> GetByDataAsync(DateOnly data, CancellationToken ct = default);

    // Bozze attive (Stato = 0, non eliminate), ordinate per UpdatedAt DESC.
    Task<IReadOnlyList<VerbaleListItem>> GetBozzeAsync(CancellationToken ct = default);
}
