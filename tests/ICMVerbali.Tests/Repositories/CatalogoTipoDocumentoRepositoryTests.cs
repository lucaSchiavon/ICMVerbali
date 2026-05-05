using ICMVerbali.Web.Repositories;

namespace ICMVerbali.Tests.Repositories;

public class CatalogoTipoDocumentoRepositoryTests
{
    private readonly TestSqlConnectionFactory _factory = new();

    [Fact]
    public async Task GetAllAttivi_returns_seeded_4_items_in_order()
    {
        var repo = new CatalogoTipoDocumentoRepository(_factory);

        var rows = await repo.GetAllAttiviAsync();

        Assert.Equal(4, rows.Count);
        Assert.Equal("DOC_NOTIFICA_PRELIMINARE", rows[0].Codice);
        Assert.Equal("DOC_ALTRO", rows[^1].Codice);
    }
}
