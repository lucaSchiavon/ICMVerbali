using Dapper;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories;

namespace ICMVerbali.Tests.Repositories;

public class CommittenteRepositoryTests
{
    private readonly TestSqlConnectionFactory _factory = new();

    [Fact]
    public async Task Create_then_GetById_returns_same_record()
    {
        var repo = new CommittenteRepository(_factory);
        var committente = new Committente
        {
            Id = Guid.CreateVersion7(),
            RagioneSociale = $"Test SpA {Guid.CreateVersion7():N}",
            Indirizzo = "Via Test 1, Roma",
            CodiceFiscale = "12345678901",
            PartitaIva = "12345678901",
            NumeroIscrizioneRegistroImprese = "RM-12345",
            IsAttivo = true,
        };

        try
        {
            await repo.CreateAsync(committente);
            var read = await repo.GetByIdAsync(committente.Id);

            Assert.NotNull(read);
            Assert.Equal(committente.Id, read!.Id);
            Assert.Equal(committente.RagioneSociale, read.RagioneSociale);
            Assert.Equal(committente.Indirizzo, read.Indirizzo);
            Assert.Equal(committente.CodiceFiscale, read.CodiceFiscale);
            Assert.True(read.IsAttivo);
        }
        finally
        {
            await using var conn = await _factory.CreateOpenConnectionAsync();
            await conn.ExecuteAsync("DELETE FROM dbo.Committente WHERE Id = @Id", new { committente.Id });
        }
    }
}
