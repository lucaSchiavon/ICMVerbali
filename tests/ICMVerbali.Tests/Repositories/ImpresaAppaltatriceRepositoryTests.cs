using Dapper;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories;

namespace ICMVerbali.Tests.Repositories;

public class ImpresaAppaltatriceRepositoryTests
{
    private readonly TestSqlConnectionFactory _factory = new();

    [Fact]
    public async Task Create_then_GetById_returns_same_record()
    {
        var repo = new ImpresaAppaltatriceRepository(_factory);
        var impresa = new ImpresaAppaltatrice
        {
            Id = Guid.CreateVersion7(),
            RagioneSociale = $"Test Srl {Guid.CreateVersion7():N}",
            Indirizzo = "Via Test 2, Milano",
            CodiceFiscale = "98765432109",
            PartitaIva = "98765432109",
            NumeroIscrizioneRegistroImprese = "MI-98765",
            IsAttivo = true,
        };

        try
        {
            await repo.CreateAsync(impresa);
            var read = await repo.GetByIdAsync(impresa.Id);

            Assert.NotNull(read);
            Assert.Equal(impresa.Id, read!.Id);
            Assert.Equal(impresa.RagioneSociale, read.RagioneSociale);
            Assert.True(read.IsAttivo);
        }
        finally
        {
            await using var conn = await _factory.CreateOpenConnectionAsync();
            await conn.ExecuteAsync("DELETE FROM dbo.ImpresaAppaltatrice WHERE Id = @Id", new { impresa.Id });
        }
    }
}
