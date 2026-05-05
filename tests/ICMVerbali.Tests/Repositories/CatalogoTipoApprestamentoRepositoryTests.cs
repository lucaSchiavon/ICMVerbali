using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Repositories;

namespace ICMVerbali.Tests.Repositories;

public class CatalogoTipoApprestamentoRepositoryTests
{
    private readonly TestSqlConnectionFactory _factory = new();

    [Fact]
    public async Task GetAllAttivi_returns_seeded_7_items_grouped_by_sottosezione()
    {
        var repo = new CatalogoTipoApprestamentoRepository(_factory);

        var rows = await repo.GetAllAttiviAsync();

        Assert.Equal(7, rows.Count);

        // Le 4 sottosezioni del PDF sono tutte presenti.
        var sottosezioni = rows.Select(r => r.Sottosezione).Distinct().OrderBy(s => s).ToList();
        Assert.Equal(
            new[]
            {
                SottosezioneApprestamento.Organizzazione,
                SottosezioneApprestamento.CaduteDallAlto,
                SottosezioneApprestamento.EmergenzeEDpi,
                SottosezioneApprestamento.Impianti,
            },
            sottosezioni);

        // Ordering: prima sottosezione 1, ultima sottosezione 4.
        Assert.Equal(SottosezioneApprestamento.Organizzazione, rows[0].Sottosezione);
        Assert.Equal(SottosezioneApprestamento.Impianti, rows[^1].Sottosezione);
    }
}
