using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class FirmaTokenRepository : IFirmaTokenRepository
{
    private readonly ISqlConnectionFactory _factory;

    public FirmaTokenRepository(ISqlConnectionFactory factory) => _factory = factory;

    private const string SqlGetByToken = @"
SELECT Id, VerbaleId, Token, ScadenzaUtc, UsatoUtc, CreatedAt
FROM dbo.FirmaToken
WHERE Token = @Token;";

    private const string SqlGetByVerbale = @"
SELECT Id, VerbaleId, Token, ScadenzaUtc, UsatoUtc, CreatedAt
FROM dbo.FirmaToken
WHERE VerbaleId = @VerbaleId
ORDER BY CreatedAt;";

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
}
