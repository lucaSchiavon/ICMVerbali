using ICMVerbali.Web.Repositories;

namespace ICMVerbali.Tests.Repositories;

public class CatalogoTipoCondizioneAmbientaleRepositoryTests
{
    private readonly TestSqlConnectionFactory _factory = new();

    [Fact]
    public async Task GetAllAttivi_returns_seeded_4_items_in_order()
    {
        var repo = new CatalogoTipoCondizioneAmbientaleRepository(_factory);

        var rows = await repo.GetAllAttiviAsync();

        Assert.Equal(4, rows.Count);
        Assert.Equal("CONDAMB_ILLUMINAZIONE", rows[0].Codice);
        Assert.Equal("CONDAMB_PULIZIA_STRADE", rows[^1].Codice);
    }
}
