using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class CatalogoTipoAttivitaRepository : ICatalogoTipoAttivitaRepository
{
    private readonly ISqlConnectionFactory _factory;

    public CatalogoTipoAttivitaRepository(ISqlConnectionFactory factory) => _factory = factory;

    private const string SqlGetById = @"
SELECT Id, Codice, Etichetta, Ordine, IsAttivo
FROM dbo.CatalogoTipoAttivita
WHERE Id = @Id;";

    private const string SqlGetAllAttive = @"
SELECT Id, Codice, Etichetta, Ordine, IsAttivo
FROM dbo.CatalogoTipoAttivita
WHERE IsAttivo = 1
ORDER BY Ordine;";

    public async Task<CatalogoTipoAttivita?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<CatalogoTipoAttivita>(
            new CommandDefinition(SqlGetById, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<CatalogoTipoAttivita>> GetAllAttiveAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<CatalogoTipoAttivita>(
            new CommandDefinition(SqlGetAllAttive, cancellationToken: ct));
        return rows.ToList();
    }
}
