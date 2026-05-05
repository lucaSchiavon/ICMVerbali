using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class CatalogoTipoDocumentoRepository : ICatalogoTipoDocumentoRepository
{
    private readonly ISqlConnectionFactory _factory;

    public CatalogoTipoDocumentoRepository(ISqlConnectionFactory factory) => _factory = factory;

    private const string SqlGetById = @"
SELECT Id, Codice, Etichetta, Ordine, IsAttivo
FROM dbo.CatalogoTipoDocumento
WHERE Id = @Id;";

    private const string SqlGetAllAttivi = @"
SELECT Id, Codice, Etichetta, Ordine, IsAttivo
FROM dbo.CatalogoTipoDocumento
WHERE IsAttivo = 1
ORDER BY Ordine;";

    public async Task<CatalogoTipoDocumento?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<CatalogoTipoDocumento>(
            new CommandDefinition(SqlGetById, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<CatalogoTipoDocumento>> GetAllAttiviAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<CatalogoTipoDocumento>(
            new CommandDefinition(SqlGetAllAttivi, cancellationToken: ct));
        return rows.ToList();
    }
}
