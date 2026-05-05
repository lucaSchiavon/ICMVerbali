using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class PersonaRepository : IPersonaRepository
{
    private readonly ISqlConnectionFactory _factory;

    public PersonaRepository(ISqlConnectionFactory factory) => _factory = factory;

    private const string SqlInsert = @"
INSERT INTO dbo.Persona (Id, Nominativo, Azienda, IsAttivo)
VALUES (@Id, @Nominativo, @Azienda, @IsAttivo);";

    private const string SqlGetById = @"
SELECT Id, Nominativo, Azienda, IsAttivo
FROM dbo.Persona
WHERE Id = @Id;";

    private const string SqlGetAttive = @"
SELECT Id, Nominativo, Azienda, IsAttivo
FROM dbo.Persona
WHERE IsAttivo = 1
ORDER BY Nominativo;";

    private const string SqlGetAll = @"
SELECT Id, Nominativo, Azienda, IsAttivo
FROM dbo.Persona
ORDER BY IsAttivo DESC, Nominativo;";

    private const string SqlUpdate = @"
UPDATE dbo.Persona
SET Nominativo = @Nominativo,
    Azienda = @Azienda,
    IsAttivo = @IsAttivo
WHERE Id = @Id;";

    public async Task CreateAsync(Persona persona, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(SqlInsert, persona, cancellationToken: ct));
    }

    public async Task UpdateAsync(Persona persona, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(SqlUpdate, persona, cancellationToken: ct));
    }

    public async Task<Persona?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Persona>(
            new CommandDefinition(SqlGetById, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Persona>> GetAttiveAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Persona>(
            new CommandDefinition(SqlGetAttive, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<Persona>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Persona>(
            new CommandDefinition(SqlGetAll, cancellationToken: ct));
        return rows.ToList();
    }
}
