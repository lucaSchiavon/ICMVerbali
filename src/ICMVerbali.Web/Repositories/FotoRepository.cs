using Dapper;
using ICMVerbali.Web.Data;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Repositories;

public sealed class FotoRepository : IFotoRepository
{
    private readonly ISqlConnectionFactory _factory;

    public FotoRepository(ISqlConnectionFactory factory) => _factory = factory;

    private const string SqlGetByVerbale = @"
SELECT Id, VerbaleId, FilePathRelativo, Didascalia, Ordine, CreatedAt
FROM dbo.Foto
WHERE VerbaleId = @VerbaleId
ORDER BY Ordine;";

    private const string SqlGetById = @"
SELECT Id, VerbaleId, FilePathRelativo, Didascalia, Ordine, CreatedAt
FROM dbo.Foto
WHERE Id = @Id;";

    private const string SqlInsert = @"
INSERT INTO dbo.Foto (Id, VerbaleId, FilePathRelativo, Didascalia, Ordine)
VALUES (@Id, @VerbaleId, @FilePathRelativo, @Didascalia, @Ordine);";

    // Calcola Ordine come max+1 nella stessa transazione per evitare race tra
    // upload concorrenti dallo stesso utente (mobile + desktop, retry, ecc.).
    private const string SqlNextOrdine = @"
SELECT ISNULL(MAX(Ordine), 0) + 1 FROM dbo.Foto WHERE VerbaleId = @VerbaleId;";

    private const string SqlUpdateDidascalia = @"
UPDATE dbo.Foto SET Didascalia = @Didascalia WHERE Id = @Id;";

    private const string SqlGetPathById = @"
SELECT FilePathRelativo FROM dbo.Foto WHERE Id = @Id;";

    private const string SqlDelete = @"
DELETE FROM dbo.Foto WHERE Id = @Id;";

    private const string SqlUpdateOrdineRow = @"
UPDATE dbo.Foto SET Ordine = @Ordine WHERE Id = @Id AND VerbaleId = @VerbaleId;";

    private const string SqlBumpVerbaleUpdatedAt = @"
UPDATE dbo.Verbale SET UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id;";

    public async Task<IReadOnlyList<Foto>> GetByVerbaleAsync(Guid verbaleId, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Foto>(
            new CommandDefinition(SqlGetByVerbale, new { VerbaleId = verbaleId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<Foto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Foto>(
            new CommandDefinition(SqlGetById, new { Id = id }, cancellationToken: ct));
    }

    public async Task CreateAsync(Foto foto, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var ordine = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                SqlNextOrdine, new { foto.VerbaleId }, transaction: tx, cancellationToken: ct));
            foto.Ordine = ordine;

            await conn.ExecuteAsync(new CommandDefinition(
                SqlInsert, foto, transaction: tx, cancellationToken: ct));

            await conn.ExecuteAsync(new CommandDefinition(
                SqlBumpVerbaleUpdatedAt, new { Id = foto.VerbaleId }, transaction: tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task UpdateDidascaliaAsync(Guid id, string? didascalia, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            SqlUpdateDidascalia, new { Id = id, Didascalia = didascalia }, cancellationToken: ct));
    }

    public async Task<string?> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var path = await conn.QuerySingleOrDefaultAsync<string?>(new CommandDefinition(
                SqlGetPathById, new { Id = id }, transaction: tx, cancellationToken: ct));
            if (path is null)
            {
                await tx.RollbackAsync(ct);
                return null;
            }

            // Recuperiamo VerbaleId per il bump.
            var verbaleId = await conn.QuerySingleAsync<Guid>(new CommandDefinition(
                "SELECT VerbaleId FROM dbo.Foto WHERE Id = @Id;",
                new { Id = id }, transaction: tx, cancellationToken: ct));

            await conn.ExecuteAsync(new CommandDefinition(
                SqlDelete, new { Id = id }, transaction: tx, cancellationToken: ct));

            await conn.ExecuteAsync(new CommandDefinition(
                SqlBumpVerbaleUpdatedAt, new { Id = verbaleId }, transaction: tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
            return path;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task UpdateOrdineBulkAsync(
        Guid verbaleId,
        IEnumerable<FotoOrdineUpdate> updates,
        CancellationToken ct = default)
    {
        var rows = updates.Select(u => new { u.Id, u.Ordine, VerbaleId = verbaleId }).ToList();
        if (rows.Count == 0) return;

        await using var conn = await _factory.CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                SqlUpdateOrdineRow, rows, transaction: tx, cancellationToken: ct));
            await conn.ExecuteAsync(new CommandDefinition(
                SqlBumpVerbaleUpdatedAt, new { Id = verbaleId }, transaction: tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
