using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
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
}
