using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Models;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Managers;

public sealed class VerbaleManager : IVerbaleManager
{
    private readonly IVerbaleRepository _repo;
    private readonly ICatalogoTipoAttivitaRepository _catAttivita;
    private readonly ICatalogoTipoDocumentoRepository _catDocumenti;
    private readonly ICatalogoTipoApprestamentoRepository _catApprestamenti;
    private readonly ICatalogoTipoCondizioneAmbientaleRepository _catCondizioniAmb;

    public VerbaleManager(
        IVerbaleRepository repo,
        ICatalogoTipoAttivitaRepository catAttivita,
        ICatalogoTipoDocumentoRepository catDocumenti,
        ICatalogoTipoApprestamentoRepository catApprestamenti,
        ICatalogoTipoCondizioneAmbientaleRepository catCondizioniAmb)
    {
        _repo = repo;
        _catAttivita = catAttivita;
        _catDocumenti = catDocumenti;
        _catApprestamenti = catApprestamenti;
        _catCondizioniAmb = catCondizioniAmb;
    }

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
        // Fetch in parallelo dei 4 cataloghi attivi (query indipendenti).
        var attivitaTask = _catAttivita.GetAllAttiveAsync(ct);
        var documentiTask = _catDocumenti.GetAllAttiviAsync(ct);
        var apprestamentiTask = _catApprestamenti.GetAllAttiviAsync(ct);
        var condizioniTask = _catCondizioniAmb.GetAllAttiviAsync(ct);
        await Task.WhenAll(attivitaTask, documentiTask, apprestamentiTask, condizioniTask);

        var verbaleId = Guid.NewGuid();

        // Numero/Anno restano null fino alla transizione FirmatoCse (vedi §9.10).
        // Esito/Meteo/Interferenze restano null nella bozza, validati alla firma.
        var verbale = new Verbale
        {
            Id = verbaleId,
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

        // Una riga per ogni voce di catalogo attiva: il wizard B.8c renderizza
        // i checkrow leggendo queste righe (non il catalogo), cosi' eventuali
        // catalogo successivamente disattivate non spariscono dalla UI del verbale.
        var attivita = attivitaTask.Result.Select(c => new VerbaleAttivita
        {
            VerbaleId = verbaleId,
            CatalogoTipoAttivitaId = c.Id,
            Selezionato = false,
            AltroDescrizione = null,
        }).ToList();

        var documenti = documentiTask.Result.Select(c => new VerbaleDocumento
        {
            VerbaleId = verbaleId,
            CatalogoTipoDocumentoId = c.Id,
            Applicabile = false,
            Conforme = false,
            Note = null,
            AltroDescrizione = null,
        }).ToList();

        var apprestamenti = apprestamentiTask.Result.Select(c => new VerbaleApprestamento
        {
            VerbaleId = verbaleId,
            CatalogoTipoApprestamentoId = c.Id,
            Applicabile = false,
            Conforme = false,
            Note = null,
        }).ToList();

        var condizioniAmbientali = condizioniTask.Result.Select(c => new VerbaleCondizioneAmbientale
        {
            VerbaleId = verbaleId,
            CatalogoTipoCondizioneAmbientaleId = c.Id,
            Conforme = false,
            NonConforme = false,
            Note = null,
        }).ToList();

        var audit = new VerbaleAudit
        {
            Id = Guid.NewGuid(),
            VerbaleId = verbaleId,
            UtenteId = compilatoDaUtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
            Note = null,
        };

        await _repo.CreateBozzaWithChildrenAsync(
            verbale, attivita, documenti, apprestamenti, condizioniAmbientali, audit, ct);

        return verbale;
    }

    public Task<Verbale?> GetAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<VerbaleListItem>> GetVerbaliDelGiornoAsync(DateOnly data, CancellationToken ct = default)
        => _repo.GetByDataAsync(data, ct);

    public Task<IReadOnlyList<VerbaleListItem>> GetBozzeAsync(CancellationToken ct = default)
        => _repo.GetBozzeAsync(ct);
}
