using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class FirmaTokenRepository : IFirmaTokenRepository
{
    private readonly ISqlConnectionFactory _factory;

    public FirmaTokenRepository(ISqlConnectionFactory factory) => _factory = factory;

    private const string SqlGetByToken = @"
SELECT Id, VerbaleId, Token, ScadenzaUtc, UsatoUtc, CreatedAt, RevocatoUtc
FROM dbo.FirmaToken
WHERE Token = @Token;";

    private const string SqlGetByVerbale = @"
SELECT Id, VerbaleId, Token, ScadenzaUtc, UsatoUtc, CreatedAt, RevocatoUtc
FROM dbo.FirmaToken
WHERE VerbaleId = @VerbaleId
ORDER BY CreatedAt;";

    // B.12: l'ultimo token utilizzabile per un verbale. Ordina DESC per CreatedAt
    // perche' rigenerare aggiunge sempre una riga piu' recente. Usa SYSUTCDATETIME()
    // server-side per la scadenza, coerente con le altre query del modulo.
    private const string SqlGetUltimoAttivo = @"
SELECT TOP 1 Id, VerbaleId, Token, ScadenzaUtc, UsatoUtc, CreatedAt, RevocatoUtc
FROM dbo.FirmaToken
WHERE VerbaleId = @VerbaleId
  AND UsatoUtc IS NULL
  AND RevocatoUtc IS NULL
  AND ScadenzaUtc > SYSUTCDATETIME()
ORDER BY CreatedAt DESC;";

    // Revoca tutti i token "attivi" del verbale. Filtra UsatoUtc IS NULL per non
    // toccare quelli gia' consumati (preserva l'evento storico) e RevocatoUtc IS
    // NULL per idempotenza in caso di rigenerazioni multiple ravvicinate.
    private const string SqlRevocaAttiviPerVerbale = @"
UPDATE dbo.FirmaToken
SET RevocatoUtc = SYSUTCDATETIME()
WHERE VerbaleId = @VerbaleId
  AND UsatoUtc IS NULL
  AND RevocatoUtc IS NULL;";

    private const string SqlInsertFirmaToken = @"
INSERT INTO dbo.FirmaToken (Id, VerbaleId, Token, ScadenzaUtc, UsatoUtc, CreatedAt)
VALUES (@Id, @VerbaleId, @Token, @ScadenzaUtc, NULL, @CreatedAt);";

    private const string SqlInsertAudit = @"
INSERT INTO dbo.VerbaleAudit (Id, VerbaleId, UtenteId, DataEvento, EventoTipo, Note)
VALUES (@Id, @VerbaleId, @UtenteId, @DataEvento, @EventoTipo, @Note);";

    public async Task<FirmaToken?> GetByTokenAsync(Guid token, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<FirmaToken>(
            new CommandDefinition(SqlGetByToken, new { Token = token }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<FirmaToken>> GetByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<FirmaToken>(
            new CommandDefinition(SqlGetByVerbale, new { VerbaleId = verbaleId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<FirmaToken?> GetUltimoAttivoAsync(Guid verbaleId, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<FirmaToken>(
            new CommandDefinition(SqlGetUltimoAttivo, new { VerbaleId = verbaleId }, cancellationToken: ct));
    }

    public async Task RigeneraAsync(
        Guid verbaleId,
        FirmaTokenInputs nuovoToken,
        Guid utenteId,
        CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            // 1. Revoca i token attivi precedenti (no-op se gia' tutti scaduti/usati/revocati).
            await conn.ExecuteAsync(new CommandDefinition(SqlRevocaAttiviPerVerbale,
                new { VerbaleId = verbaleId },
                transaction: tx, cancellationToken: ct));

            // 2. Inserisci il nuovo token.
            await conn.ExecuteAsync(new CommandDefinition(SqlInsertFirmaToken, new
            {
                Id = nuovoToken.TokenId,
                VerbaleId = verbaleId,
                Token = nuovoToken.Token,
                ScadenzaUtc = nuovoToken.ScadenzaUtc,
                CreatedAt = nuovoToken.CreatedAt,
            }, transaction: tx, cancellationToken: ct));

            // 3. Audit. Note minimale: il TokenId del nuovo link e' utile per tracciare
            // dal log applicativo; il Token GUID non viene loggato per non esporlo.
            await conn.ExecuteAsync(new CommandDefinition(SqlInsertAudit, new
            {
                Id = Guid.CreateVersion7(),
                VerbaleId = verbaleId,
                UtenteId = utenteId,
                DataEvento = nuovoToken.CreatedAt,
                EventoTipo = EventoAuditTipo.RigenerazioneToken,
                Note = (string?)$"Rigenerato magic-link impresa (nuovo TokenId {nuovoToken.TokenId}).",
            }, transaction: tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
