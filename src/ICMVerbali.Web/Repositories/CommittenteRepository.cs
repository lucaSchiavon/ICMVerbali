using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class CommittenteRepository : ICommittenteRepository
{
    private readonly ISqlConnectionFactory _factory;

    public CommittenteRepository(ISqlConnectionFactory factory) => _factory = factory;

    private const string SqlInsert = @"
INSERT INTO dbo.Committente
    (Id, RagioneSociale, Indirizzo, CodiceFiscale, PartitaIva, NumeroIscrizioneRegistroImprese, IsAttivo)
VALUES
    (@Id, @RagioneSociale, @Indirizzo, @CodiceFiscale, @PartitaIva, @NumeroIscrizioneRegistroImprese, @IsAttivo);";

    private const string SqlGetById = @"
SELECT Id, RagioneSociale, Indirizzo, CodiceFiscale, PartitaIva, NumeroIscrizioneRegistroImprese, IsAttivo
FROM dbo.Committente
WHERE Id = @Id;";

    private const string SqlGetAttivi = @"
SELECT Id, RagioneSociale, Indirizzo, CodiceFiscale, PartitaIva, NumeroIscrizioneRegistroImprese, IsAttivo
FROM dbo.Committente
WHERE IsAttivo = 1
ORDER BY RagioneSociale;";

    public async Task CreateAsync(Committente committente, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(SqlInsert, committente, cancellationToken: ct));
    }

    public async Task<Committente?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Committente>(
            new CommandDefinition(SqlGetById, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Committente>> GetAttiviAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Committente>(
            new CommandDefinition(SqlGetAttivi, cancellationToken: ct));
        return rows.ToList();
    }
}
