using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Repositories.Interfaces;

// Repository dell'aggregate root Verbale. Per ora espone solo il minimo per
// B.5 (create base + read by id). Le entita' figlie (Presenza, Foto, Firma,
// VerbaleAttivita ecc.) e le transizioni di stato verranno aggiunte in B.7+
// quando ci servira' il wizard. Vedi docs/01-design.md §2.4.
public interface IVerbaleRepository
{
    Task CreateAsync(Verbale verbale, CancellationToken ct = default);
    Task<Verbale?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
