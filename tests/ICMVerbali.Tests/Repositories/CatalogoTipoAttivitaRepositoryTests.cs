using ICMVerbali.Web.Repositories;

namespace ICMVerbali.Tests.Repositories;

public class CatalogoTipoAttivitaRepositoryTests
{
    private readonly TestSqlConnectionFactory _factory = new();

    [Fact]
    public async Task GetAllAttive_returns_seeded_16_items_in_order()
    {
        var repo = new CatalogoTipoAttivitaRepository(_factory);

        var rows = await repo.GetAllAttiveAsync();

        Assert.Equal(16, rows.Count);
        Assert.Equal("ATTIVITA_ALLESTIMENTO_SMOBILIZZO", rows[0].Codice);
        Assert.Equal("ATTIVITA_ALTRO", rows[^1].Codice);

        // GetById round-trip su una voce nota
        var byId = await repo.GetByIdAsync(rows[0].Id);
        Assert.NotNull(byId);
        Assert.Equal(rows[0].Codice, byId!.Codice);
    }
}
