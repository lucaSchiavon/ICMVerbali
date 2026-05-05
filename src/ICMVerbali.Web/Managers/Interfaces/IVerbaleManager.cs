using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Managers.Interfaces;

// Manager dell'aggregate root Verbale. Espone solo la creazione bozza base + read.
// Le entita' figlie (Presenza, Foto, Firma ecc.) e le transizioni di stato
// (Bozza -> FirmatoCse -> ...) verranno aggiunte in B.7+ quando ci servira'
// il wizard.
public interface IVerbaleManager
{
    Task<Verbale> CreaBozzaAsync(
        DateOnly data,
        Guid cantiereId,
        Guid committenteId,
        Guid impresaAppaltatriceId,
        Guid rlPersonaId,
        Guid cspPersonaId,
        Guid csePersonaId,
        Guid dlPersonaId,
        Guid compilatoDaUtenteId,
        CancellationToken ct = default);

    Task<Verbale?> GetAsync(Guid id, CancellationToken ct = default);
}
