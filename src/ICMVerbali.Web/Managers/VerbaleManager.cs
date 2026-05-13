using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Models;
using ICMVerbali.Web.Repositories.Interfaces;
using ICMVerbali.Web.Storage;

namespace ICMVerbali.Web.Managers;

public sealed class VerbaleManager : IVerbaleManager
{
    private readonly IVerbaleRepository _repo;
    private readonly ICatalogoTipoAttivitaRepository _catAttivita;
    private readonly ICatalogoTipoDocumentoRepository _catDocumenti;
    private readonly ICatalogoTipoApprestamentoRepository _catApprestamenti;
    private readonly ICatalogoTipoCondizioneAmbientaleRepository _catCondizioniAmb;
    private readonly IFirmaStorageService _firmaStorage;
    private readonly IFirmaTokenManager _firmaTokenManager;
    private readonly TimeProvider _clock;

    public VerbaleManager(
        IVerbaleRepository repo,
        ICatalogoTipoAttivitaRepository catAttivita,
        ICatalogoTipoDocumentoRepository catDocumenti,
        ICatalogoTipoApprestamentoRepository catApprestamenti,
        ICatalogoTipoCondizioneAmbientaleRepository catCondizioniAmb,
        IFirmaStorageService firmaStorage,
        IFirmaTokenManager firmaTokenManager,
        TimeProvider clock)
    {
        _repo = repo;
        _catAttivita = catAttivita;
        _catDocumenti = catDocumenti;
        _catApprestamenti = catApprestamenti;
        _catCondizioniAmb = catCondizioniAmb;
        _firmaStorage = firmaStorage;
        _firmaTokenManager = firmaTokenManager;
        _clock = clock;
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

        var verbaleId = Guid.CreateVersion7();

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
            Id = Guid.CreateVersion7(),
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

    public Task UpdateAnagraficaAsync(
        Guid id,
        DateOnly data,
        Guid cantiereId,
        Guid committenteId,
        Guid impresaAppaltatriceId,
        Guid rlPersonaId,
        Guid cspPersonaId,
        Guid csePersonaId,
        Guid dlPersonaId,
        CancellationToken ct = default)
        => _repo.UpdateAnagraficaAsync(id, data, cantiereId, committenteId, impresaAppaltatriceId,
            rlPersonaId, cspPersonaId, csePersonaId, dlPersonaId, ct);

    public Task UpdateMeteoEsitoAsync(
        Guid id,
        EsitoVerifica? esito,
        CondizioneMeteo? meteo,
        int? temperaturaCelsius,
        CancellationToken ct = default)
        => _repo.UpdateMeteoEsitoAsync(id, esito, meteo, temperaturaCelsius, ct);

    public Task UpdateInterferenzeAsync(
        Guid id,
        GestioneInterferenze? interferenze,
        string? interferenzeNote,
        CancellationToken ct = default)
        => _repo.UpdateInterferenzeAsync(id, interferenze, interferenzeNote, ct);

    public Task<IReadOnlyList<VerbaleListItem>> GetVerbaliDelGiornoAsync(DateOnly data, CancellationToken ct = default)
        => _repo.GetByDataAsync(data, ct);

    public Task<IReadOnlyList<VerbaleListItem>> GetBozzeAsync(CancellationToken ct = default)
        => _repo.GetBozzeAsync(ct);

    // -------- checklist (step 3-6 wizard) -------------------------------

    public Task<IReadOnlyList<VerbaleAttivitaItem>>
        GetAttivitaAsync(Guid verbaleId, CancellationToken ct = default)
        => _repo.GetAttivitaByVerbaleAsync(verbaleId, ct);

    public Task<IReadOnlyList<VerbaleDocumentoItem>>
        GetDocumentiAsync(Guid verbaleId, CancellationToken ct = default)
        => _repo.GetDocumentiByVerbaleAsync(verbaleId, ct);

    public Task<IReadOnlyList<VerbaleApprestamentoItem>>
        GetApprestamentiAsync(Guid verbaleId, CancellationToken ct = default)
        => _repo.GetApprestamentiByVerbaleAsync(verbaleId, ct);

    public Task<IReadOnlyList<VerbaleCondizioneAmbientaleItem>>
        GetCondizioniAsync(Guid verbaleId, CancellationToken ct = default)
        => _repo.GetCondizioniByVerbaleAsync(verbaleId, ct);

    public Task UpdateAttivitaAsync(
        Guid verbaleId, IEnumerable<VerbaleAttivita> rows, CancellationToken ct = default)
        => _repo.UpdateAttivitaBulkAsync(verbaleId, rows, ct);

    public Task UpdateDocumentiAsync(
        Guid verbaleId, IEnumerable<VerbaleDocumento> rows, CancellationToken ct = default)
        => _repo.UpdateDocumentiBulkAsync(verbaleId, rows, ct);

    public Task UpdateApprestamentiAsync(
        Guid verbaleId, IEnumerable<VerbaleApprestamento> rows, CancellationToken ct = default)
        => _repo.UpdateApprestamentiBulkAsync(verbaleId, rows, ct);

    public Task UpdateCondizioniAsync(
        Guid verbaleId, IEnumerable<VerbaleCondizioneAmbientale> rows, CancellationToken ct = default)
        => _repo.UpdateCondizioniBulkAsync(verbaleId, rows, ct);

    // -------- prescrizioni (step 8) --------------------------------------

    public Task<IReadOnlyList<PrescrizioneCse>>
        GetPrescrizioniAsync(Guid verbaleId, CancellationToken ct = default)
        => _repo.GetPrescrizioniByVerbaleAsync(verbaleId, ct);

    public Task UpdatePrescrizioniAsync(
        Guid verbaleId, IEnumerable<PrescrizioneCse> rows, CancellationToken ct = default)
    {
        // Normalizza: scarta righe con Testo vuoto/null, rinumera Ordine 1..N
        // sull'ordine in cui arrivano (la UI controlla l'ordine con move up/down).
        // Forza VerbaleId al valore passato per difesa (mai fidarsi del client).
        // Genera nuovi Id per righe nuove (Id == Guid.Empty) e per qualsiasi Id
        // duplicato all'interno della stessa lista (vista 2026-05-13: una race
        // tra OnBlur autosave e Submit click puo' ripresentare lo stesso Id due
        // volte, causando PK violation in ReplacePrescrizioniAsync. La difesa qui
        // garantisce che il batch INSERT non incontri mai duplicati anche se
        // l'UI dovesse essere bacata).
        var seen = new HashSet<Guid>();
        var normalized = new List<PrescrizioneCse>();
        var ordine = 0;
        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.Testo)) continue;
            ordine++;
            var id = r.Id;
            if (id == Guid.Empty || !seen.Add(id))
            {
                id = Guid.CreateVersion7();
                seen.Add(id);
            }
            normalized.Add(new PrescrizioneCse
            {
                Id = id,
                VerbaleId = verbaleId,
                Testo = r.Testo.Trim(),
                Ordine = ordine,
            });
        }

        return _repo.ReplacePrescrizioniAsync(verbaleId, normalized, ct);
    }

    // -------- firma CSE (Bozza -> FirmatoCse) ----------------------------
    public async Task<FirmaCseResult> FirmaCseAsync(
        Guid verbaleId,
        string nomeFirmatario,
        byte[] pngBytes,
        Guid utenteId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pngBytes);
        if (pngBytes.Length == 0)
            throw new ArgumentException("Immagine firma vuota.", nameof(pngBytes));
        if (string.IsNullOrWhiteSpace(nomeFirmatario))
            throw new ArgumentException("Nome firmatario obbligatorio.", nameof(nomeFirmatario));

        // 1. Carica verbale e verifica stato.
        var verbale = await _repo.GetByIdAsync(verbaleId, ct)
            ?? throw new InvalidOperationException($"Verbale {verbaleId} non trovato.");
        if (verbale.Stato != StatoVerbale.Bozza)
            throw new InvalidOperationException(
                $"Verbale {verbaleId} non e' in Bozza (stato attuale: {verbale.Stato}).");

        // 2. Validazione hard: anagrafiche + esito + meteo.
        var validation = VerbaleValidator.PuoFirmare(verbale);
        if (!validation.IsValid)
            throw new VerbaleNonFirmabileException(validation.Errori);

        // 3. Salva PNG su storage prima della transazione DB. Se fallisce, niente
        // riga di firma su DB. Se la transazione DB poi fallisce, il PNG resta
        // orfano sul filesystem: accettabile (GC futuro, pattern di B.9 per le foto).
        var storageResult = await _firmaStorage.SalvaAsync(
            verbaleId, TipoFirmatario.Cse, pngBytes, ct);

        // 4. Anno della firma = anno corrente UTC. La numerazione si resetta al
        // 1 gennaio. Una bozza creata a fine dicembre ma firmata a gennaio prende
        // il numero del nuovo anno (decisione design 2026-05-13).
        var now = _clock.GetUtcNow().UtcDateTime;
        var anno = now.Year;
        var dataFirma = DateOnly.FromDateTime(now);

        // 5. Pre-calcola il token impresa: TokenId, Token (GUID v7), ScadenzaUtc
        // (now + ScadenzaOreDefault da appsettings). L'INSERT effettivo avviene
        // dentro la transazione del repo per garantire atomicita' con la firma.
        var tokenSeed = _firmaTokenManager.CalcolaProssimoToken();
        var tokenInputs = new FirmaTokenInputs(
            tokenSeed.TokenId,
            tokenSeed.Token,
            tokenSeed.ScadenzaUtc,
            tokenSeed.CreatedAt);

        // 6. Transazione DB: Numero/Anno + Firma + UPDATE stato + audit + token impresa.
        return await _repo.FirmaCseAsync(
            verbaleId, anno, nomeFirmatario.Trim(), dataFirma,
            storageResult.FilePathRelativo, utenteId, tokenInputs, ct);
    }

    // -------- firma Impresa (FirmatoCse -> FirmatoImpresa) --------------
    public async Task FirmaImpresaAsync(
        Guid token,
        string nomeFirmatario,
        byte[] pngBytes,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pngBytes);
        if (pngBytes.Length == 0)
            throw new ArgumentException("Immagine firma vuota.", nameof(pngBytes));
        if (string.IsNullOrWhiteSpace(nomeFirmatario))
            throw new ArgumentException("Nome firmatario obbligatorio.", nameof(nomeFirmatario));

        // 1. Valida token (FirmaTokenInvalidoException con motivo specifico se KO).
        var tokenEntity = await _firmaTokenManager.ValidaTokenAsync(token, ct);

        // 2. Carica verbale e verifica stato. Il check ridondante con il lock del
        // repo serve per fail-fast e per messaggio piu' pulito alla UI.
        var verbale = await _repo.GetByIdAsync(tokenEntity.VerbaleId, ct)
            ?? throw new InvalidOperationException(
                $"Verbale {tokenEntity.VerbaleId} (associato al token) non trovato.");
        if (verbale.Stato != StatoVerbale.FirmatoCse)
            throw new InvalidOperationException(
                $"Verbale {verbale.Id} non e' in FirmatoCse (stato attuale: {verbale.Stato}).");

        // 3. Salva PNG su storage prima della transazione. Path:
        // firme/{verbaleId}/impresa.png. In caso di rollback DB, il PNG resta
        // orfano sul filesystem (accettato, pattern B.9/B.10).
        var storageResult = await _firmaStorage.SalvaAsync(
            verbale.Id, TipoFirmatario.ImpresaAppaltatrice, pngBytes, ct);

        var now = _clock.GetUtcNow().UtcDateTime;
        var dataFirma = DateOnly.FromDateTime(now);

        // 4. Transazione DB: insert Firma + UPDATE stato + audit + mark token usato.
        // UtenteId per l'audit = CompilatoDaUtenteId del verbale (vedi design doc).
        await _repo.FirmaImpresaAsync(
            verbale.Id, tokenEntity.Id, nomeFirmatario.Trim(), dataFirma,
            storageResult.FilePathRelativo, verbale.CompilatoDaUtenteId, ct);
    }
}
