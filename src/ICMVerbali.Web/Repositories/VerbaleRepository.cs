using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class VerbaleRepository : IVerbaleRepository
{
    private readonly ISqlConnectionFactory _factory;

    public VerbaleRepository(ISqlConnectionFactory factory) => _factory = factory;

    // CreatedAt/UpdatedAt non passati: i default SYSUTCDATETIME() li valorizzano.
    private const string SqlInsert = @"
INSERT INTO dbo.Verbale (
    Id, Numero, Anno, Data,
    CantiereId, CommittenteId, ImpresaAppaltatriceId,
    RlPersonaId, CspPersonaId, CsePersonaId, DlPersonaId,
    Esito, Meteo, TemperaturaCelsius, Interferenze, InterferenzeNote,
    Stato, CompilatoDaUtenteId, IsDeleted, DeletedAt
)
VALUES (
    @Id, @Numero, @Anno, @Data,
    @CantiereId, @CommittenteId, @ImpresaAppaltatriceId,
    @RlPersonaId, @CspPersonaId, @CsePersonaId, @DlPersonaId,
    @Esito, @Meteo, @TemperaturaCelsius, @Interferenze, @InterferenzeNote,
    @Stato, @CompilatoDaUtenteId, @IsDeleted, @DeletedAt
);";

    private const string SqlGetById = @"
SELECT
    Id, Numero, Anno, Data,
    CantiereId, CommittenteId, ImpresaAppaltatriceId,
    RlPersonaId, CspPersonaId, CsePersonaId, DlPersonaId,
    Esito, Meteo, TemperaturaCelsius, Interferenze, InterferenzeNote,
    Stato, CompilatoDaUtenteId, IsDeleted, DeletedAt,
    CreatedAt, UpdatedAt
FROM dbo.Verbale
WHERE Id = @Id;";

    public async Task CreateAsync(Verbale verbale, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(SqlInsert, verbale, cancellationToken: ct));
    }

    public async Task<Verbale?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Verbale>(
            new CommandDefinition(SqlGetById, new { Id = id }, cancellationToken: ct));
    }
}
