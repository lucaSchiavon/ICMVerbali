using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Repositories.Interfaces;

// Repository per la tabella FirmaToken (B.11). La CREAZIONE di un token avviene
// dentro la transazione di VerbaleRepository.FirmaCseAsync (atomico con la firma
// CSE), quindi qui esponiamo solo letture e il MarkUsato. Vedi docs/01-design.md
// Addendum 2026-05-14.
public interface IFirmaTokenRepository
{
    // Lookup del token per la pagina /firma-impresa/{token}.
    Task<FirmaToken?> GetByTokenAsync(Guid token, CancellationToken ct = default);

    // Tutti i token emessi per un verbale (per debug / detail view / rigenerazione futura).
    Task<IReadOnlyList<FirmaToken>> GetByVerbaleAsync(Guid verbaleId, CancellationToken ct = default);
}
