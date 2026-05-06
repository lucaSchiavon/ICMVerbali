using Dapper;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Repositories;

namespace ICMVerbali.Tests.Repositories;

public class VerbaleRepositoryTests
{
    private readonly TestSqlConnectionFactory _factory = new();

    [Fact]
    public async Task CreateBozzaWithChildren_inserts_verbale_and_all_children_in_transaction()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();

        // Una sola riga per tabella checklist: il test verifica la transazione,
        // non la cardinalita' del catalogo (gia' coperta dai test catalogo).
        var verbaleId = Guid.NewGuid();
        var verbale = BuildBozza(verbaleId, anagrafiche);

        var attivita = new[]
        {
            new VerbaleAttivita
            {
                VerbaleId = verbaleId,
                CatalogoTipoAttivitaId = await GetFirstCatalogoIdAsync("CatalogoTipoAttivita"),
                Selezionato = false,
            }
        };
        var documenti = new[]
        {
            new VerbaleDocumento
            {
                VerbaleId = verbaleId,
                CatalogoTipoDocumentoId = await GetFirstCatalogoIdAsync("CatalogoTipoDocumento"),
                Applicabile = false,
                Conforme = false,
            }
        };
        var apprestamenti = new[]
        {
            new VerbaleApprestamento
            {
                VerbaleId = verbaleId,
                CatalogoTipoApprestamentoId = await GetFirstCatalogoIdAsync("CatalogoTipoApprestamento"),
                Applicabile = false,
                Conforme = false,
            }
        };
        var condizioniAmbientali = new[]
        {
            new VerbaleCondizioneAmbientale
            {
                VerbaleId = verbaleId,
                CatalogoTipoCondizioneAmbientaleId = await GetFirstCatalogoIdAsync("CatalogoTipoCondizioneAmbientale"),
                Conforme = false,
                NonConforme = false,
            }
        };
        var audit = new VerbaleAudit
        {
            Id = Guid.NewGuid(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };

        try
        {
            await verbaleRepo.CreateBozzaWithChildrenAsync(
                verbale, attivita, documenti, apprestamenti, condizioniAmbientali, audit);

            var read = await verbaleRepo.GetByIdAsync(verbaleId);
            Assert.NotNull(read);
            Assert.Equal(StatoVerbale.Bozza, read!.Stato);
            Assert.Null(read.Numero);
            Assert.Null(read.Anno);

            // Conta righe figlie: una per tabella, piu' la riga di audit.
            await using var conn = await _factory.CreateOpenConnectionAsync();
            Assert.Equal(1, await CountAsync(conn, "VerbaleAttivita", verbaleId));
            Assert.Equal(1, await CountAsync(conn, "VerbaleDocumento", verbaleId));
            Assert.Equal(1, await CountAsync(conn, "VerbaleApprestamento", verbaleId));
            Assert.Equal(1, await CountAsync(conn, "VerbaleCondizioneAmbientale", verbaleId));
            Assert.Equal(1, await CountAsync(conn, "VerbaleAudit", verbaleId));
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    [Fact]
    public async Task GetBozzeAsync_includes_created_bozza_with_joined_anagrafiche()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();

        var verbaleId = Guid.NewGuid();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.NewGuid(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };

        try
        {
            await verbaleRepo.CreateBozzaWithChildrenAsync(
                verbale,
                Array.Empty<VerbaleAttivita>(),
                Array.Empty<VerbaleDocumento>(),
                Array.Empty<VerbaleApprestamento>(),
                Array.Empty<VerbaleCondizioneAmbientale>(),
                audit);

            var bozze = await verbaleRepo.GetBozzeAsync();
            var item = bozze.FirstOrDefault(b => b.Id == verbaleId);

            Assert.NotNull(item);
            Assert.Equal(StatoVerbale.Bozza, item!.Stato);
            Assert.Null(item.Numero);
            Assert.Null(item.Anno);
            Assert.Equal(anagrafiche.CantiereUbicazione, item.CantiereUbicazione);
            Assert.Equal(anagrafiche.CommittenteRagioneSociale, item.CommittenteRagioneSociale);
            Assert.Equal(anagrafiche.ImpresaRagioneSociale, item.ImpresaAppaltatriceRagioneSociale);
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    [Fact]
    public async Task GetByDataAsync_excludes_bozze()
    {
        // Le bozze devono comparire SOLO in GetBozzeAsync, non nella lista del giorno
        // (Home: due sezioni distinte). Questo test blinda quella separazione.
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();

        var verbaleId = Guid.NewGuid();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.NewGuid(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };

        try
        {
            await verbaleRepo.CreateBozzaWithChildrenAsync(
                verbale,
                Array.Empty<VerbaleAttivita>(),
                Array.Empty<VerbaleDocumento>(),
                Array.Empty<VerbaleApprestamento>(),
                Array.Empty<VerbaleCondizioneAmbientale>(),
                audit);

            var deldgg = await verbaleRepo.GetByDataAsync(verbale.Data);
            Assert.DoesNotContain(deldgg, b => b.Id == verbaleId);
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    [Fact]
    public async Task UpdateAnagrafica_changes_data_and_fk_then_round_trip_reads_back()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.NewGuid();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.NewGuid(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };

        // Crea un secondo cantiere a cui spostare il verbale (cleanup nel finally).
        var altroCantiereId = Guid.NewGuid();
        var altroCantiere = new Cantiere
        {
            Id = altroCantiereId,
            Ubicazione = $"Test loc ALT {Guid.NewGuid():N}",
            Tipologia = "Test",
            IsAttivo = true,
        };

        try
        {
            await verbaleRepo.CreateBozzaWithChildrenAsync(
                verbale,
                Array.Empty<VerbaleAttivita>(),
                Array.Empty<VerbaleDocumento>(),
                Array.Empty<VerbaleApprestamento>(),
                Array.Empty<VerbaleCondizioneAmbientale>(),
                audit);

            await new CantiereRepository(_factory).CreateAsync(altroCantiere);

            var nuovaData = verbale.Data.AddDays(-3);
            await verbaleRepo.UpdateAnagraficaAsync(
                verbaleId, nuovaData,
                altroCantiereId, anagrafiche.CommittenteId, anagrafiche.ImpresaId,
                anagrafiche.PersonaId, anagrafiche.PersonaId, anagrafiche.PersonaId, anagrafiche.PersonaId);

            var read = await verbaleRepo.GetByIdAsync(verbaleId);
            Assert.NotNull(read);
            Assert.Equal(nuovaData, read!.Data);
            Assert.Equal(altroCantiereId, read.CantiereId);
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
            await using var conn = await _factory.CreateOpenConnectionAsync();
            await conn.ExecuteAsync("DELETE FROM dbo.Cantiere WHERE Id = @Id", new { Id = altroCantiereId });
        }
    }

    [Fact]
    public async Task UpdateMeteoEsito_writes_nullable_fields_and_round_trip_reads_back()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.NewGuid();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.NewGuid(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };

        try
        {
            await verbaleRepo.CreateBozzaWithChildrenAsync(
                verbale,
                Array.Empty<VerbaleAttivita>(),
                Array.Empty<VerbaleDocumento>(),
                Array.Empty<VerbaleApprestamento>(),
                Array.Empty<VerbaleCondizioneAmbientale>(),
                audit);

            await verbaleRepo.UpdateMeteoEsitoAsync(
                verbaleId,
                EsitoVerifica.NcMinori,
                CondizioneMeteo.Pioggia,
                temperaturaCelsius: 18);

            var read = await verbaleRepo.GetByIdAsync(verbaleId);
            Assert.NotNull(read);
            Assert.Equal(EsitoVerifica.NcMinori, read!.Esito);
            Assert.Equal(CondizioneMeteo.Pioggia, read.Meteo);
            Assert.Equal(18, read.TemperaturaCelsius);
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    [Fact]
    public async Task UpdateInterferenze_writes_then_round_trip_reads_back()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.NewGuid();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.NewGuid(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };

        try
        {
            await verbaleRepo.CreateBozzaWithChildrenAsync(
                verbale,
                Array.Empty<VerbaleAttivita>(),
                Array.Empty<VerbaleDocumento>(),
                Array.Empty<VerbaleApprestamento>(),
                Array.Empty<VerbaleCondizioneAmbientale>(),
                audit);

            await verbaleRepo.UpdateInterferenzeAsync(
                verbaleId,
                GestioneInterferenze.ConAreeEsterne,
                "Lavorazioni adiacenti su strada pubblica.");

            var read = await verbaleRepo.GetByIdAsync(verbaleId);
            Assert.NotNull(read);
            Assert.Equal(GestioneInterferenze.ConAreeEsterne, read!.Interferenze);
            Assert.Equal("Lavorazioni adiacenti su strada pubblica.", read.InterferenzeNote);
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    [Fact]
    public async Task UpdateAttivitaBulk_then_GetByVerbale_returns_joined_updated_state()
    {
        // Crea bozza con 1 riga in VerbaleAttivita (catalogo reale), poi
        // toggla Selezionato + AltroDescrizione, e verifica via GET joinato.
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.NewGuid();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var catalogoId = await GetFirstCatalogoIdAsync("CatalogoTipoAttivita");
        var attivita = new[]
        {
            new VerbaleAttivita
            {
                VerbaleId = verbaleId,
                CatalogoTipoAttivitaId = catalogoId,
                Selezionato = false,
                AltroDescrizione = null,
            }
        };
        var audit = new VerbaleAudit
        {
            Id = Guid.NewGuid(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };

        try
        {
            await verbaleRepo.CreateBozzaWithChildrenAsync(
                verbale, attivita,
                Array.Empty<VerbaleDocumento>(),
                Array.Empty<VerbaleApprestamento>(),
                Array.Empty<VerbaleCondizioneAmbientale>(),
                audit);

            // Toggla a Selezionato=true, aggiorna in transazione.
            var updated = new[]
            {
                new VerbaleAttivita
                {
                    VerbaleId = verbaleId,
                    CatalogoTipoAttivitaId = catalogoId,
                    Selezionato = true,
                    AltroDescrizione = "Voce di test",
                }
            };
            await verbaleRepo.UpdateAttivitaBulkAsync(verbaleId, updated);

            var read = await verbaleRepo.GetAttivitaByVerbaleAsync(verbaleId);
            Assert.Single(read);
            Assert.True(read[0].Selezionato);
            Assert.Equal("Voce di test", read[0].AltroDescrizione);
            Assert.False(string.IsNullOrEmpty(read[0].Etichetta));
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    // --------- helpers ----------------------------------------------------

    private sealed record AnagraficheSeed(
        Guid CantiereId, string CantiereUbicazione,
        Guid CommittenteId, string CommittenteRagioneSociale,
        Guid ImpresaId, string ImpresaRagioneSociale,
        Guid PersonaId,
        Guid UtenteId);

    private async Task<AnagraficheSeed> SeedAnagraficheAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var cantiere = new Cantiere { Id = Guid.NewGuid(), Ubicazione = $"Test loc {suffix}", Tipologia = "Test", IsAttivo = true };
        var committente = new Committente { Id = Guid.NewGuid(), RagioneSociale = $"Test Cmt {suffix}", IsAttivo = true };
        var impresa = new ImpresaAppaltatrice { Id = Guid.NewGuid(), RagioneSociale = $"Test Imp {suffix}", IsAttivo = true };
        var persona = new Persona { Id = Guid.NewGuid(), Nominativo = $"Test Persona {suffix}", Azienda = "ICM", IsAttivo = true };
        var utente = new Utente
        {
            Id = Guid.NewGuid(),
            Username = $"user-{suffix}",
            PasswordHash = "fake",
            Ruolo = RuoloUtente.Cse,
            IsAttivo = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await new CantiereRepository(_factory).CreateAsync(cantiere);
        await new CommittenteRepository(_factory).CreateAsync(committente);
        await new ImpresaAppaltatriceRepository(_factory).CreateAsync(impresa);
        await new PersonaRepository(_factory).CreateAsync(persona);
        await new UtenteRepository(_factory).CreateAsync(utente);

        return new AnagraficheSeed(
            cantiere.Id, cantiere.Ubicazione,
            committente.Id, committente.RagioneSociale,
            impresa.Id, impresa.RagioneSociale,
            persona.Id,
            utente.Id);
    }

    private static Verbale BuildBozza(Guid id, AnagraficheSeed a) => new()
    {
        Id = id,
        Numero = null,
        Anno = null,
        Data = DateOnly.FromDateTime(DateTime.UtcNow),
        CantiereId = a.CantiereId,
        CommittenteId = a.CommittenteId,
        ImpresaAppaltatriceId = a.ImpresaId,
        RlPersonaId = a.PersonaId,
        CspPersonaId = a.PersonaId,
        CsePersonaId = a.PersonaId,
        DlPersonaId = a.PersonaId,
        Esito = null,
        Meteo = null,
        TemperaturaCelsius = null,
        Interferenze = null,
        InterferenzeNote = null,
        Stato = StatoVerbale.Bozza,
        CompilatoDaUtenteId = a.UtenteId,
        IsDeleted = false,
        DeletedAt = null,
    };

    private async Task<Guid> GetFirstCatalogoIdAsync(string tableName)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync();
        return await conn.QueryFirstAsync<Guid>(
            $"SELECT TOP 1 Id FROM dbo.{tableName} WHERE IsAttivo = 1 ORDER BY Ordine;");
    }

    private static async Task<int> CountAsync(System.Data.Common.DbConnection conn, string table, Guid verbaleId)
        => await conn.QuerySingleAsync<int>(
            $"SELECT COUNT(*) FROM dbo.{table} WHERE VerbaleId = @VerbaleId",
            new { VerbaleId = verbaleId });

    private async Task CleanupAsync(Guid verbaleId, AnagraficheSeed a)
    {
        // Ordine: prima Verbale (cascade libera figlie e audit),
        // poi anagrafiche (FK NO ACTION richiede ordine esplicito).
        await using var conn = await _factory.CreateOpenConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM dbo.Verbale WHERE Id = @Id", new { Id = verbaleId });
        await conn.ExecuteAsync("DELETE FROM dbo.Utente WHERE Id = @Id", new { Id = a.UtenteId });
        await conn.ExecuteAsync("DELETE FROM dbo.Persona WHERE Id = @Id", new { Id = a.PersonaId });
        await conn.ExecuteAsync("DELETE FROM dbo.ImpresaAppaltatrice WHERE Id = @Id", new { Id = a.ImpresaId });
        await conn.ExecuteAsync("DELETE FROM dbo.Committente WHERE Id = @Id", new { Id = a.CommittenteId });
        await conn.ExecuteAsync("DELETE FROM dbo.Cantiere WHERE Id = @Id", new { Id = a.CantiereId });
    }
}
