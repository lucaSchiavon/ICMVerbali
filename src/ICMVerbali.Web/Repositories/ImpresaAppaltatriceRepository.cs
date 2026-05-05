using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class ImpresaAppaltatriceRepository : IImpresaAppaltatriceRepository
{
    private readonly ISqlConnectionFactory _factory;

    public ImpresaAppaltatriceRepository(ISqlConnectionFactory factory) => _factory = factory;

    private const string SqlInsert = @"
INSERT INTO dbo.ImpresaAppaltatrice
    (Id, RagioneSociale, Indirizzo, CodiceFiscale, PartitaIva, NumeroIscrizioneRegistroImprese, IsAttivo)
VALUES
    (@Id, @RagioneSociale, @Indirizzo, @CodiceFiscale, @PartitaIva, @NumeroIscrizioneRegistroImprese, @IsAttivo);";

    private const string SqlGetById = @"
SELECT Id, RagioneSociale, Indirizzo, CodiceFiscale, PartitaIva, NumeroIscrizioneRegistroImprese, IsAttivo
FROM dbo.ImpresaAppaltatrice
WHERE Id = @Id;";

    private const string SqlGetAttivi = @"
SELECT Id, RagioneSociale, Indirizzo, CodiceFiscale, PartitaIva, NumeroIscrizioneRegistroImprese, IsAttivo
FROM dbo.ImpresaAppaltatrice
WHERE IsAttivo = 1
ORDER BY RagioneSociale;";

    private const string SqlGetAll = @"
SELECT Id, RagioneSociale, Indirizzo, CodiceFiscale, PartitaIva, NumeroIscrizioneRegistroImprese, IsAttivo
FROM dbo.ImpresaAppaltatrice
ORDER BY IsAttivo DESC, RagioneSociale;";

    private const string SqlUpdate = @"
UPDATE dbo.ImpresaAppaltatrice
SET RagioneSociale = @RagioneSociale,
    Indirizzo = @Indirizzo,
    CodiceFiscale = @CodiceFiscale,
    PartitaIva = @PartitaIva,
    NumeroIscrizioneRegistroImprese = @NumeroIscrizioneRegistroImprese,
    IsAttivo = @IsAttivo
WHERE Id = @Id;";

    public async Task CreateAsync(ImpresaAppaltatrice impresa, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(SqlInsert, impresa, cancellationToken: ct));
    }

    public async Task UpdateAsync(ImpresaAppaltatrice impresa, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(SqlUpdate, impresa, cancellationToken: ct));
    }

    public async Task<ImpresaAppaltatrice?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ImpresaAppaltatrice>(
            new CommandDefinition(SqlGetById, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ImpresaAppaltatrice>> GetAttiviAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<ImpresaAppaltatrice>(
            new CommandDefinition(SqlGetAttivi, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ImpresaAppaltatrice>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<ImpresaAppaltatrice>(
            new CommandDefinition(SqlGetAll, cancellationToken: ct));
        return rows.ToList();
    }
}
