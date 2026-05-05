using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class CatalogoTipoApprestamentoRepository : ICatalogoTipoApprestamentoRepository
{
    private readonly ISqlConnectionFactory _factory;

    public CatalogoTipoApprestamentoRepository(ISqlConnectionFactory factory) => _factory = factory;

    private const string SqlGetById = @"
SELECT Id, Codice, Etichetta, Ordine, IsAttivo, Sottosezione
FROM dbo.CatalogoTipoApprestamento
WHERE Id = @Id;";

    private const string SqlGetAllAttivi = @"
SELECT Id, Codice, Etichetta, Ordine, IsAttivo, Sottosezione
FROM dbo.CatalogoTipoApprestamento
WHERE IsAttivo = 1
ORDER BY Sottosezione, Ordine;";

    public async Task<CatalogoTipoApprestamento?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<CatalogoTipoApprestamento>(
            new CommandDefinition(SqlGetById, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<CatalogoTipoApprestamento>> GetAllAttiviAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<CatalogoTipoApprestamento>(
            new CommandDefinition(SqlGetAllAttivi, cancellationToken: ct));
        return rows.ToList();
    }
}
