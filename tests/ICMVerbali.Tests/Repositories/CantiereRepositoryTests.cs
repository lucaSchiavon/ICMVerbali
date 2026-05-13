using Dapper;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories;

namespace ICMVerbali.Tests.Repositories;

public class CantiereRepositoryTests
{
    private readonly TestSqlConnectionFactory _factory = new();

    [Fact]
    public async Task Create_then_GetById_returns_same_record()
    {
        var repo = new CantiereRepository(_factory);
        var cantiere = new Cantiere
        {
            Id = Guid.CreateVersion7(),
            Ubicazione = $"Test ubicazione {Guid.CreateVersion7():N}",
            Tipologia = "Test tipologia",
            ImportoAppalto = 12345.67m,
            IsAttivo = true,
        };

        try
        {
            await repo.CreateAsync(cantiere);
            var read = await repo.GetByIdAsync(cantiere.Id);

            Assert.NotNull(read);
            Assert.Equal(cantiere.Id, read!.Id);
            Assert.Equal(cantiere.Ubicazione, read.Ubicazione);
            Assert.Equal(cantiere.Tipologia, read.Tipologia);
            Assert.Equal(cantiere.ImportoAppalto, read.ImportoAppalto);
            Assert.True(read.IsAttivo);
        }
        finally
        {
            await using var conn = await _factory.CreateOpenConnectionAsync();
            await conn.ExecuteAsync("DELETE FROM dbo.Cantiere WHERE Id = @Id", new { cantiere.Id });
        }
    }
}
