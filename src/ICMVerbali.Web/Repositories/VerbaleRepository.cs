using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Models;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class VerbaleRepository : IVerbaleRepository
{
    private readonly ISqlConnectionFactory _factory;

    public VerbaleRepository(ISqlConnectionFactory factory) => _factory = factory;

    // CreatedAt/UpdatedAt non passati: i default SYSUTCDATETIME() li valorizzano.
    private const string SqlInsertVerbale = @"
INSERT INTO dbo.Verbale (
    Id, Numero, Anno, Data,
    CantiereId, CommittenteId, ImpresaAppaltatriceId,
    RlPersonaId, CspPersonaId, CsePersonaId, DlPersonaId,
    Esito, Meteo, TemperaturaCelsius, Interferenze, InterferenzeNote,
    Stato, CompilatoDaUtenteId, IsDeleted, DeletedAt
)
VALUES (
    @Id, @Numero, @Anno, @Data,
    @CantiereId, @CommittenteId, @ImpresaAppaltatriceId,
    @RlPersonaId, @CspPersonaId, @CsePersonaId, @DlPersonaId,
    @Esito, @Meteo, @TemperaturaCelsius, @Interferenze, @InterferenzeNote,
    @Stato, @CompilatoDaUtenteId, @IsDeleted, @DeletedAt
);";

    private const string SqlInsertAttivita = @"
INSERT INTO dbo.VerbaleAttivita (VerbaleId, CatalogoTipoAttivitaId, Selezionato, AltroDescrizione)
VALUES (@VerbaleId, @CatalogoTipoAttivitaId, @Selezionato, @AltroDescrizione);";

    private const string SqlInsertDocumento = @"
INSERT INTO dbo.VerbaleDocumento (VerbaleId, CatalogoTipoDocumentoId, Applicabile, Conforme, Note, AltroDescrizione)
VALUES (@VerbaleId, @CatalogoTipoDocumentoId, @Applicabile, @Conforme, @Note, @AltroDescrizione);";

    private const string SqlInsertApprestamento = @"
INSERT INTO dbo.VerbaleApprestamento (VerbaleId, CatalogoTipoApprestamentoId, Applicabile, Conforme, Note)
VALUES (@VerbaleId, @CatalogoTipoApprestamentoId, @Applicabile, @Conforme, @Note);";

    private const string SqlInsertCondizioneAmbientale = @"
INSERT INTO dbo.VerbaleCondizioneAmbientale (VerbaleId, CatalogoTipoCondizioneAmbientaleId, Conforme, NonConforme, Note)
VALUES (@VerbaleId, @CatalogoTipoCondizioneAmbientaleId, @Conforme, @NonConforme, @Note);";

    // DataEvento ha default SYSUTCDATETIME() in DB ma lo passiamo esplicito per
    // averlo coerente con il timestamp logico della creazione bozza.
    private const string SqlInsertAudit = @"
INSERT INTO dbo.VerbaleAudit (Id, VerbaleId, UtenteId, DataEvento, EventoTipo, Note)
VALUES (@Id, @VerbaleId, @UtenteId, @DataEvento, @EventoTipo, @Note);";

    private const string SqlUpdateAnagrafica = @"
UPDATE dbo.Verbale
SET Data = @Data,
    CantiereId = @CantiereId,
    CommittenteId = @CommittenteId,
    ImpresaAppaltatriceId = @ImpresaAppaltatriceId,
    RlPersonaId = @RlPersonaId,
    CspPersonaId = @CspPersonaId,
    CsePersonaId = @CsePersonaId,
    DlPersonaId = @DlPersonaId,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id;";

    private const string SqlUpdateMeteoEsito = @"
UPDATE dbo.Verbale
SET Esito = @Esito,
    Meteo = @Meteo,
    TemperaturaCelsius = @TemperaturaCelsius,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id;";

    private const string SqlUpdateInterferenze = @"
UPDATE dbo.Verbale
SET Interferenze = @Interferenze,
    InterferenzeNote = @InterferenzeNote,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id;";

    // ---------- bulk update checklist (step 3-6) ------------------------

    private const string SqlUpdateAttivitaRow = @"
UPDATE dbo.VerbaleAttivita
SET Selezionato = @Selezionato,
    AltroDescrizione = @AltroDescrizione
WHERE VerbaleId = @VerbaleId AND CatalogoTipoAttivitaId = @CatalogoTipoAttivitaId;";

    private const string SqlUpdateDocumentoRow = @"
UPDATE dbo.VerbaleDocumento
SET Applicabile = @Applicabile,
    Conforme = @Conforme,
    Note = @Note,
    AltroDescrizione = @AltroDescrizione
WHERE VerbaleId = @VerbaleId AND CatalogoTipoDocumentoId = @CatalogoTipoDocumentoId;";

    private const string SqlUpdateApprestamentoRow = @"
UPDATE dbo.VerbaleApprestamento
SET Applicabile = @Applicabile,
    Conforme = @Conforme,
    Note = @Note
WHERE VerbaleId = @VerbaleId AND CatalogoTipoApprestamentoId = @CatalogoTipoApprestamentoId;";

    private const string SqlUpdateCondizioneRow = @"
UPDATE dbo.VerbaleCondizioneAmbientale
SET Conforme = @Conforme,
    NonConforme = @NonConforme,
    Note = @Note
WHERE VerbaleId = @VerbaleId AND CatalogoTipoCondizioneAmbientaleId = @CatalogoTipoCondizioneAmbientaleId;";

    private const string SqlBumpVerbaleUpdatedAt = @"
UPDATE dbo.Verbale SET UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id;";

    // ---------- prescrizioni CSE (step 8 wizard) -----------------------
    private const string SqlGetPrescrizioniByVerbale = @"
SELECT Id, VerbaleId, Testo, Ordine
FROM dbo.PrescrizioneCse
WHERE VerbaleId = @VerbaleId
ORDER BY Ordine;";

    private const string SqlDeletePrescrizioniByVerbale = @"
DELETE FROM dbo.PrescrizioneCse WHERE VerbaleId = @VerbaleId;";

    private const string SqlInsertPrescrizione = @"
INSERT INTO dbo.PrescrizioneCse (Id, VerbaleId, Testo, Ordine)
VALUES (@Id, @VerbaleId, @Testo, @Ordine);";

    // ---------- GET joinate (step 3-6 read) -----------------------------

    private const string SqlGetAttivitaByVerbale = @"
SELECT
    va.CatalogoTipoAttivitaId,
    c.Codice,
    c.Etichetta,
    c.Ordine,
    va.Selezionato,
    va.AltroDescrizione
FROM dbo.VerbaleAttivita AS va
INNER JOIN dbo.CatalogoTipoAttivita AS c ON c.Id = va.CatalogoTipoAttivitaId
WHERE va.VerbaleId = @VerbaleId
ORDER BY c.Ordine;";

    private const string SqlGetDocumentiByVerbale = @"
SELECT
    vd.CatalogoTipoDocumentoId,
    c.Codice,
    c.Etichetta,
    c.Ordine,
    vd.Applicabile,
    vd.Conforme,
    vd.Note,
    vd.AltroDescrizione
FROM dbo.VerbaleDocumento AS vd
INNER JOIN dbo.CatalogoTipoDocumento AS c ON c.Id = vd.CatalogoTipoDocumentoId
WHERE vd.VerbaleId = @VerbaleId
ORDER BY c.Ordine;";

    private const string SqlGetApprestamentiByVerbale = @"
SELECT
    va.CatalogoTipoApprestamentoId,
    c.Codice,
    c.Etichetta,
    c.Ordine,
    c.Sottosezione,
    va.Applicabile,
    va.Conforme,
    va.Note
FROM dbo.VerbaleApprestamento AS va
INNER JOIN dbo.CatalogoTipoApprestamento AS c ON c.Id = va.CatalogoTipoApprestamentoId
WHERE va.VerbaleId = @VerbaleId
ORDER BY c.Sottosezione, c.Ordine;";

    private const string SqlGetCondizioniByVerbale = @"
SELECT
    vc.CatalogoTipoCondizioneAmbientaleId,
    c.Codice,
    c.Etichetta,
    c.Ordine,
    vc.Conforme,
    vc.NonConforme,
    vc.Note
FROM dbo.VerbaleCondizioneAmbientale AS vc
INNER JOIN dbo.CatalogoTipoCondizioneAmbientale AS c ON c.Id = vc.CatalogoTipoCondizioneAmbientaleId
WHERE vc.VerbaleId = @VerbaleId
ORDER BY c.Ordine;";

    private const string SqlGetById = @"
SELECT
    Id, Numero, Anno, Data,
    CantiereId, CommittenteId, ImpresaAppaltatriceId,
    RlPersonaId, CspPersonaId, CsePersonaId, DlPersonaId,
    Esito, Meteo, TemperaturaCelsius, Interferenze, InterferenzeNote,
    Stato, CompilatoDaUtenteId, IsDeleted, DeletedAt,
    CreatedAt, UpdatedAt
FROM dbo.Verbale
WHERE Id = @Id;";

    // JOIN con anagrafiche per popolare le colonne testuali del DTO. Filtra
    // soft-deleted e seleziona solo i verbali firmati (Stato > 0). UpdatedAt DESC
    // mette in cima quelli appena modificati / firmati.
    private const string SqlGetByData = @"
SELECT
    v.Id,
    v.Numero,
    v.Anno,
    v.Data,
    c.Ubicazione                  AS CantiereUbicazione,
    cm.RagioneSociale             AS CommittenteRagioneSociale,
    ia.RagioneSociale             AS ImpresaAppaltatriceRagioneSociale,
    v.Stato,
    v.UpdatedAt
FROM dbo.Verbale AS v
INNER JOIN dbo.Cantiere             AS c  ON c.Id  = v.CantiereId
INNER JOIN dbo.Committente          AS cm ON cm.Id = v.CommittenteId
INNER JOIN dbo.ImpresaAppaltatrice  AS ia ON ia.Id = v.ImpresaAppaltatriceId
WHERE v.Data = @Data
  AND v.IsDeleted = 0
  AND v.Stato > 0
ORDER BY v.UpdatedAt DESC;";

    // Bozze (Stato = 0). UpdatedAt DESC: l'ultima toccata in cima.
    private const string SqlGetBozze = @"
SELECT
    v.Id,
    v.Numero,
    v.Anno,
    v.Data,
    c.Ubicazione                  AS CantiereUbicazione,
    cm.RagioneSociale             AS CommittenteRagioneSociale,
    ia.RagioneSociale             AS ImpresaAppaltatriceRagioneSociale,
    v.Stato,
    v.UpdatedAt
FROM dbo.Verbale AS v
INNER JOIN dbo.Cantiere             AS c  ON c.Id  = v.CantiereId
INNER JOIN dbo.Committente          AS cm ON cm.Id = v.CommittenteId
INNER JOIN dbo.ImpresaAppaltatrice  AS ia ON ia.Id = v.ImpresaAppaltatriceId
WHERE v.Stato = 0
  AND v.IsDeleted = 0
ORDER BY v.UpdatedAt DESC;";

    public async Task CreateBozzaWithChildrenAsync(
        Verbale verbale,
        IEnumerable<VerbaleAttivita> attivita,
        IEnumerable<VerbaleDocumento> documenti,
        IEnumerable<VerbaleApprestamento> apprestamenti,
        IEnumerable<VerbaleCondizioneAmbientale> condizioniAmbientali,
        VerbaleAudit audit,
        CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                SqlInsertVerbale, verbale, transaction: tx, cancellationToken: ct));

            // ExecuteAsync con IEnumerable fa multi-exec: una INSERT per riga.
            // Per i volumi previsti (decine di righe per bozza) e' adeguato.
            await conn.ExecuteAsync(new CommandDefinition(
                SqlInsertAttivita, attivita, transaction: tx, cancellationToken: ct));

            await conn.ExecuteAsync(new CommandDefinition(
                SqlInsertDocumento, documenti, transaction: tx, cancellationToken: ct));

            await conn.ExecuteAsync(new CommandDefinition(
                SqlInsertApprestamento, apprestamenti, transaction: tx, cancellationToken: ct));

            await conn.ExecuteAsync(new CommandDefinition(
                SqlInsertCondizioneAmbientale, condizioniAmbientali, transaction: tx, cancellationToken: ct));

            await conn.ExecuteAsync(new CommandDefinition(
                SqlInsertAudit, audit, transaction: tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<Verbale?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Verbale>(
            new CommandDefinition(SqlGetById, new { Id = id }, cancellationToken: ct));
    }

    public async Task UpdateAnagraficaAsync(
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
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(SqlUpdateAnagrafica, new
        {
            Id = id,
            Data = data,
            CantiereId = cantiereId,
            CommittenteId = committenteId,
            ImpresaAppaltatriceId = impresaAppaltatriceId,
            RlPersonaId = rlPersonaId,
            CspPersonaId = cspPersonaId,
            CsePersonaId = csePersonaId,
            DlPersonaId = dlPersonaId,
        }, cancellationToken: ct));
    }

    public async Task UpdateMeteoEsitoAsync(
        Guid id,
        Entities.Enums.EsitoVerifica? esito,
        Entities.Enums.CondizioneMeteo? meteo,
        int? temperaturaCelsius,
        CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(SqlUpdateMeteoEsito, new
        {
            Id = id,
            Esito = esito,
            Meteo = meteo,
            TemperaturaCelsius = temperaturaCelsius,
        }, cancellationToken: ct));
    }

    public async Task UpdateInterferenzeAsync(
        Guid id,
        Entities.Enums.GestioneInterferenze? interferenze,
        string? interferenzeNote,
        CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(SqlUpdateInterferenze, new
        {
            Id = id,
            Interferenze = interferenze,
            InterferenzeNote = interferenzeNote,
        }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<VerbaleListItem>> GetByDataAsync(DateOnly data, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<VerbaleListItem>(
            new CommandDefinition(SqlGetByData, new { Data = data }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<VerbaleListItem>> GetBozzeAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<VerbaleListItem>(
            new CommandDefinition(SqlGetBozze, cancellationToken: ct));
        return rows.ToList();
    }

    // -------- firma CSE (Bozza -> FirmatoCse) ----------------------------
    // UPDLOCK + HOLDLOCK sul SELECT iniziale serializza i firmatari concorrenti
    // sullo stesso anno (l'UNIQUE filtrato e' la rete di sicurezza finale, ma
    // il lock evita l'errore 2627 nel path comune).
    private const string SqlSelectStatoForUpdate = @"
SELECT Stato
FROM dbo.Verbale WITH (UPDLOCK, HOLDLOCK)
WHERE Id = @Id;";

    private const string SqlSelectMaxNumeroAnno = @"
SELECT ISNULL(MAX(Numero), 0)
FROM dbo.Verbale WITH (UPDLOCK, HOLDLOCK)
WHERE Anno = @Anno AND Numero IS NOT NULL;";

    private const string SqlInsertFirma = @"
INSERT INTO dbo.Firma (Id, VerbaleId, Tipo, NomeFirmatario, DataFirma, ImmagineFirmaPath)
VALUES (@Id, @VerbaleId, @Tipo, @NomeFirmatario, @DataFirma, @ImmagineFirmaPath);";

    private const string SqlUpdateVerbaleFirmaCse = @"
UPDATE dbo.Verbale
SET Stato = @Stato,
    Numero = @Numero,
    Anno = @Anno,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id;";

    public async Task<FirmaCseResult> FirmaCseAsync(
        Guid verbaleId,
        int anno,
        string nomeFirmatario,
        DateOnly dataFirma,
        string immagineFirmaPath,
        Guid utenteId,
        CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            // 1. Lock + verifica Stato == Bozza. Se gia' firmato (race tra due
            // click), il chiamante riceve InvalidOperationException e abortisce.
            var statoCorrente = await conn.QuerySingleOrDefaultAsync<byte?>(
                new CommandDefinition(SqlSelectStatoForUpdate,
                    new { Id = verbaleId },
                    transaction: tx, cancellationToken: ct));
            if (statoCorrente is null)
                throw new InvalidOperationException($"Verbale {verbaleId} non trovato.");
            if (statoCorrente != (byte)StatoVerbale.Bozza)
                throw new InvalidOperationException(
                    $"Verbale {verbaleId} non e' in Bozza (stato attuale: {(StatoVerbale)statoCorrente.Value}).");

            // 2. Numero progressivo per l'anno corrente. UPDLOCK su MAX impedisce
            // a un secondo firmatario di calcolare lo stesso numero finche'
            // questa transazione non committa.
            var maxNumero = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(SqlSelectMaxNumeroAnno,
                    new { Anno = anno },
                    transaction: tx, cancellationToken: ct));
            var numero = maxNumero + 1;

            // 3. INSERT in Firma (UQ_Firma_VerbaleId_Tipo blocca doppie firme CSE).
            await conn.ExecuteAsync(new CommandDefinition(SqlInsertFirma, new
            {
                Id = Guid.NewGuid(),
                VerbaleId = verbaleId,
                Tipo = TipoFirmatario.Cse,
                NomeFirmatario = nomeFirmatario,
                DataFirma = dataFirma,
                ImmagineFirmaPath = immagineFirmaPath,
            }, transaction: tx, cancellationToken: ct));

            // 4. UPDATE Verbale -> FirmatoCse.
            await conn.ExecuteAsync(new CommandDefinition(SqlUpdateVerbaleFirmaCse, new
            {
                Id = verbaleId,
                Stato = StatoVerbale.FirmatoCse,
                Numero = numero,
                Anno = anno,
            }, transaction: tx, cancellationToken: ct));

            // 5. Audit.
            await conn.ExecuteAsync(new CommandDefinition(SqlInsertAudit, new
            {
                Id = Guid.NewGuid(),
                VerbaleId = verbaleId,
                UtenteId = utenteId,
                DataEvento = DateTime.UtcNow,
                EventoTipo = EventoAuditTipo.Firma,
                Note = (string?)$"Firma CSE: {nomeFirmatario}",
            }, transaction: tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
            return new FirmaCseResult(numero, anno);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // -------- checklist GET (step 3-6) -----------------------------------

    public async Task<IReadOnlyList<VerbaleAttivitaItem>>
        GetAttivitaByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<VerbaleAttivitaItem>(
            new CommandDefinition(SqlGetAttivitaByVerbale, new { VerbaleId = verbaleId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<VerbaleDocumentoItem>>
        GetDocumentiByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<VerbaleDocumentoItem>(
            new CommandDefinition(SqlGetDocumentiByVerbale, new { VerbaleId = verbaleId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<VerbaleApprestamentoItem>>
        GetApprestamentiByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<VerbaleApprestamentoItem>(
            new CommandDefinition(SqlGetApprestamentiByVerbale, new { VerbaleId = verbaleId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<VerbaleCondizioneAmbientaleItem>>
        GetCondizioniByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<VerbaleCondizioneAmbientaleItem>(
            new CommandDefinition(SqlGetCondizioniByVerbale, new { VerbaleId = verbaleId }, cancellationToken: ct));
        return rows.ToList();
    }

    // -------- checklist UPDATE bulk (step 3-6) ---------------------------
    // Una transazione: UPDATE multipli sulla checklist + bump UpdatedAt sul Verbale.

    public Task UpdateAttivitaBulkAsync(
        Guid verbaleId, IEnumerable<VerbaleAttivita> rows, CancellationToken ct = default)
        => RunBulkUpdateAsync(verbaleId, rows, SqlUpdateAttivitaRow, ct);

    public Task UpdateDocumentiBulkAsync(
        Guid verbaleId, IEnumerable<VerbaleDocumento> rows, CancellationToken ct = default)
        => RunBulkUpdateAsync(verbaleId, rows, SqlUpdateDocumentoRow, ct);

    public Task UpdateApprestamentiBulkAsync(
        Guid verbaleId, IEnumerable<VerbaleApprestamento> rows, CancellationToken ct = default)
        => RunBulkUpdateAsync(verbaleId, rows, SqlUpdateApprestamentoRow, ct);

    public Task UpdateCondizioniBulkAsync(
        Guid verbaleId, IEnumerable<VerbaleCondizioneAmbientale> rows, CancellationToken ct = default)
        => RunBulkUpdateAsync(verbaleId, rows, SqlUpdateCondizioneRow, ct);

    // -------- prescrizioni (step 8) --------------------------------------

    public async Task<IReadOnlyList<PrescrizioneCse>>
        GetPrescrizioniByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<PrescrizioneCse>(
            new CommandDefinition(SqlGetPrescrizioniByVerbale, new { VerbaleId = verbaleId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task ReplacePrescrizioniAsync(
        Guid verbaleId, IEnumerable<PrescrizioneCse> rows, CancellationToken ct = default)
    {
        var list = rows.ToList();
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                SqlDeletePrescrizioniByVerbale, new { VerbaleId = verbaleId }, transaction: tx, cancellationToken: ct));

            if (list.Count > 0)
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    SqlInsertPrescrizione, list, transaction: tx, cancellationToken: ct));
            }

            await conn.ExecuteAsync(new CommandDefinition(
                SqlBumpVerbaleUpdatedAt, new { Id = verbaleId }, transaction: tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task RunBulkUpdateAsync<T>(
        Guid verbaleId, IEnumerable<T> rows, string sqlUpdateRow, CancellationToken ct)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            // Dapper con IEnumerable -> N execute. Sui volumi attesi (max ~30
            // righe per checklist) il costo e' trascurabile.
            await conn.ExecuteAsync(new CommandDefinition(
                sqlUpdateRow, rows, transaction: tx, cancellationToken: ct));
            await conn.ExecuteAsync(new CommandDefinition(
                SqlBumpVerbaleUpdatedAt, new { Id = verbaleId }, transaction: tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
