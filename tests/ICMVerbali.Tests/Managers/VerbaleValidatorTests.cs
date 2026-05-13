using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Managers;

namespace ICMVerbali.Tests.Managers;

public class VerbaleValidatorTests
{
    [Fact]
    public void PuoFirmare_verbale_completo_e_valido()
    {
        var verbale = BuildCompleto();
        var result = VerbaleValidator.PuoFirmare(verbale);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errori);
    }

    [Fact]
    public void PuoFirmare_senza_Esito_segnala_errore()
    {
        var verbale = BuildCompleto();
        verbale.Esito = null;
        var result = VerbaleValidator.PuoFirmare(verbale);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errori, e => e.Contains("Esito", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PuoFirmare_senza_Meteo_segnala_errore()
    {
        var verbale = BuildCompleto();
        verbale.Meteo = null;
        var result = VerbaleValidator.PuoFirmare(verbale);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errori, e => e.Contains("meteo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PuoFirmare_senza_anagrafiche_segnala_tutti_gli_errori()
    {
        var verbale = BuildCompleto();
        verbale.CantiereId = Guid.Empty;
        verbale.CommittenteId = Guid.Empty;
        verbale.ImpresaAppaltatriceId = Guid.Empty;
        verbale.RlPersonaId = Guid.Empty;
        verbale.CspPersonaId = Guid.Empty;
        verbale.CsePersonaId = Guid.Empty;
        verbale.DlPersonaId = Guid.Empty;
        var result = VerbaleValidator.PuoFirmare(verbale);
        Assert.False(result.IsValid);
        // 7 anagrafiche mancanti -> 7 errori.
        Assert.Equal(7, result.Errori.Count);
    }

    [Fact]
    public void PuoFirmare_senza_temperatura_e_prescrizioni_e_foto_resta_valido()
    {
        // Vincolo minimo (2026-05-13): solo anagrafiche + Esito + Meteo. Temperatura
        // null, foto/prescrizioni assenti, interferenze libere — tutto ok.
        var verbale = BuildCompleto();
        verbale.TemperaturaCelsius = null;
        verbale.Interferenze = null;
        verbale.InterferenzeNote = null;
        var result = VerbaleValidator.PuoFirmare(verbale);
        Assert.True(result.IsValid);
    }

    private static Verbale BuildCompleto() => new()
    {
        Id = Guid.CreateVersion7(),
        Data = DateOnly.FromDateTime(DateTime.UtcNow),
        CantiereId = Guid.CreateVersion7(),
        CommittenteId = Guid.CreateVersion7(),
        ImpresaAppaltatriceId = Guid.CreateVersion7(),
        RlPersonaId = Guid.CreateVersion7(),
        CspPersonaId = Guid.CreateVersion7(),
        CsePersonaId = Guid.CreateVersion7(),
        DlPersonaId = Guid.CreateVersion7(),
        Esito = EsitoVerifica.Conforme,
        Meteo = CondizioneMeteo.Sereno,
        TemperaturaCelsius = 22,
        Stato = StatoVerbale.Bozza,
        CompilatoDaUtenteId = Guid.CreateVersion7(),
    };
}
