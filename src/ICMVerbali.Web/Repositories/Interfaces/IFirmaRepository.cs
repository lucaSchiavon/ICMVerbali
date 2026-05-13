using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;

namespace ICMVerbali.Web.Repositories.Interfaces;

// Repository di sola lettura per le firme: la SCRITTURA della firma CSE e'
// incapsulata in VerbaleRepository.FirmaCseAsync (transazione cross-tabella).
// Qui esponiamo i GET serve dall'endpoint /api/firme/{verbaleId}/{tipo}.
public interface IFirmaRepository
{
    Task<IReadOnlyList<Firma>> GetByVerbaleAsync(Guid verbaleId, CancellationToken ct = default);

    Task<Firma?> GetByVerbaleAndTipoAsync(
        Guid verbaleId, TipoFirmatario tipo, CancellationToken ct = default);
}
