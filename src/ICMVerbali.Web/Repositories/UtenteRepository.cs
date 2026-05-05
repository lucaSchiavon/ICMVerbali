using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class UtenteRepository : IUtenteRepository
{
    private readonly ISqlConnectionFactory _factory;

    public UtenteRepository(ISqlConnectionFactory factory) => _factory = factory;

    // CreatedAt/UpdatedAt non passati: il default SYSUTCDATETIME() del DB li valorizza.
    private const string SqlInsert = @"
INSERT INTO dbo.Utente (Id, Username, Email, PasswordHash, Ruolo, IsAttivo)
VALUES (@Id, @Username, @Email, @PasswordHash, @Ruolo, @IsAttivo);";

    private const string SqlGetById = @"
SELECT Id, Username, Email, PasswordHash, Ruolo, IsAttivo, CreatedAt, UpdatedAt
FROM dbo.Utente
WHERE Id = @Id;";

    private const string SqlGetByUsername = @"
SELECT Id, Username, Email, PasswordHash, Ruolo, IsAttivo, CreatedAt, UpdatedAt
FROM dbo.Utente
WHERE Username = @Username;";

    private const string SqlGetAttivi = @"
SELECT Id, Username, Email, PasswordHash, Ruolo, IsAttivo, CreatedAt, UpdatedAt
FROM dbo.Utente
WHERE IsAttivo = 1
ORDER BY Username;";

    public async Task CreateAsync(Utente utente, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(SqlInsert, utente, cancellationToken: ct));
    }

    public async Task<Utente?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Utente>(
            new CommandDefinition(SqlGetById, new { Id = id }, cancellationToken: ct));
    }

    public async Task<Utente?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Utente>(
            new CommandDefinition(SqlGetByUsername, new { Username = username }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Utente>> GetAttiviAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Utente>(
            new CommandDefinition(SqlGetAttivi, cancellationToken: ct));
        return rows.ToList();
    }
}
