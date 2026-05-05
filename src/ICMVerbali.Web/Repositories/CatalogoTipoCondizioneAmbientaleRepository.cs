using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class CatalogoTipoCondizioneAmbientaleRepository : ICatalogoTipoCondizioneAmbientaleRepository
{
    private readonly ISqlConnectionFactory _factory;

    public CatalogoTipoCondizioneAmbientaleRepository(ISqlConnectionFactory factory) => _factory = factory;

    private const string SqlGetById = @"
SELECT Id, Codice, Etichetta, Ordine, IsAttivo
FROM dbo.CatalogoTipoCondizioneAmbientale
WHERE Id = @Id;";

    private const string SqlGetAllAttivi = @"
SELECT Id, Codice, Etichetta, Ordine, IsAttivo
FROM dbo.CatalogoTipoCondizioneAmbientale
WHERE IsAttivo = 1
ORDER BY Ordine;";

    public async Task<CatalogoTipoCondizioneAmbientale?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<CatalogoTipoCondizioneAmbientale>(
            new CommandDefinition(SqlGetById, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<CatalogoTipoCondizioneAmbientale>> GetAllAttiviAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<CatalogoTipoCondizioneAmbientale>(
            new CommandDefinition(SqlGetAllAttivi, cancellationToken: ct));
        return rows.ToList();
    }
}
