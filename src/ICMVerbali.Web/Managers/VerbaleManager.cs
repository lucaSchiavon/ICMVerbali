using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Managers;

public sealed class VerbaleManager : IVerbaleManager
{
    private readonly IVerbaleRepository _repo;

    public VerbaleManager(IVerbaleRepository repo) => _repo = repo;

    public async Task<Verbale> CreaBozzaAsync(
        DateOnly data,
        Guid cantiereId,
        Guid committenteId,
        Guid impresaAppaltatriceId,
        Guid rlPersonaId,
        Guid cspPersonaId,
        Guid csePersonaId,
        Guid dlPersonaId,
        Guid compilatoDaUtenteId,
        CancellationToken ct = default)
    {
        // Numero/Anno restano null fino alla transizione FirmatoCse (vedi §9.10).
        // Esito/Meteo/Interferenze restano null nella bozza, validati alla firma.
        var verbale = new Verbale
        {
            Id = Guid.NewGuid(),
            Numero = null,
            Anno = null,
            Data = data,
            CantiereId = cantiereId,
            CommittenteId = committenteId,
            ImpresaAppaltatriceId = impresaAppaltatriceId,
            RlPersonaId = rlPersonaId,
            CspPersonaId = cspPersonaId,
            CsePersonaId = csePersonaId,
            DlPersonaId = dlPersonaId,
            Esito = null,
            Meteo = null,
            TemperaturaCelsius = null,
            Interferenze = null,
            InterferenzeNote = null,
            Stato = StatoVerbale.Bozza,
            CompilatoDaUtenteId = compilatoDaUtenteId,
            IsDeleted = false,
            DeletedAt = null,
        };
        await _repo.CreateAsync(verbale, ct);
        return verbale;
    }

    public Task<Verbale?> GetAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);
}
