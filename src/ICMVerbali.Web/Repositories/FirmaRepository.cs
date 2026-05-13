using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class FirmaRepository : IFirmaRepository
{
    private readonly ISqlConnectionFactory _factory;

    public FirmaRepository(ISqlConnectionFactory factory) => _factory = factory;

    private const string SqlGetByVerbale = @"
SELECT Id, VerbaleId, Tipo, NomeFirmatario, DataFirma, ImmagineFirmaPath
FROM dbo.Firma
WHERE VerbaleId = @VerbaleId
ORDER BY Tipo;";

    private const string SqlGetByVerbaleAndTipo = @"
SELECT Id, VerbaleId, Tipo, NomeFirmatario, DataFirma, ImmagineFirmaPath
FROM dbo.Firma
WHERE VerbaleId = @VerbaleId AND Tipo = @Tipo;";

    public async Task<IReadOnlyList<Firma>> GetByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Firma>(
            new CommandDefinition(SqlGetByVerbale, new { VerbaleId = verbaleId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<Firma?> GetByVerbaleAndTipoAsync(
        Guid verbaleId, TipoFirmatario tipo, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Firma>(
            new CommandDefinition(
                SqlGetByVerbaleAndTipo,
                new { VerbaleId = verbaleId, Tipo = tipo },
                cancellationToken: ct));
    }
}
