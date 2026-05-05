using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class CantiereRepository : ICantiereRepository
{
    private readonly ISqlConnectionFactory _factory;

    public CantiereRepository(ISqlConnectionFactory factory) => _factory = factory;

    private const string SqlInsert = @"
INSERT INTO dbo.Cantiere (Id, Ubicazione, Tipologia, ImportoAppalto, IsAttivo)
VALUES (@Id, @Ubicazione, @Tipologia, @ImportoAppalto, @IsAttivo);";

    private const string SqlGetById = @"
SELECT Id, Ubicazione, Tipologia, ImportoAppalto, IsAttivo
FROM dbo.Cantiere
WHERE Id = @Id;";

    private const string SqlGetAttivi = @"
SELECT Id, Ubicazione, Tipologia, ImportoAppalto, IsAttivo
FROM dbo.Cantiere
WHERE IsAttivo = 1
ORDER BY Ubicazione;";

    public async Task CreateAsync(Cantiere cantiere, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(SqlInsert, cantiere, cancellationToken: ct));
    }

    public async Task<Cantiere?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Cantiere>(
            new CommandDefinition(SqlGetById, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Cantiere>> GetAttiviAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Cantiere>(
            new CommandDefinition(SqlGetAttivi, cancellationToken: ct));
        return rows.ToList();
    }
}
