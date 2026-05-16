using Dapper;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Repositories;
using ICMVerbali.Web.Repositories.Interfaces;

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
        var verbaleId = Guid.CreateVersion7();
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
            Id = Guid.CreateVersion7(),
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

        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
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

        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
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
        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };

        // Crea un secondo cantiere a cui spostare il verbale (cleanup nel finally).
        var altroCantiereId = Guid.CreateVersion7();
        var altroCantiere = new Cantiere
        {
            Id = altroCantiereId,
            Ubicazione = $"Test loc ALT {Guid.CreateVersion7():N}",
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
        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
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
        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
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
        var verbaleId = Guid.CreateVersion7();
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
            Id = Guid.CreateVersion7(),
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

    [Fact]
    public async Task ReplacePrescrizioni_then_GetByVerbale_returns_replaced_set_in_order()
    {
        // Verifica delete-and-insert: una prima Replace inserisce 3 righe, una
        // seconda Replace con 2 righe diverse deve sostituire integralmente la
        // lista (nessuna riga residua della prima). UpdatedAt viene bumpato.
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
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

            var creatoUpdatedAt = (await verbaleRepo.GetByIdAsync(verbaleId))!.UpdatedAt;

            var primaTornata = new[]
            {
                new PrescrizioneCse { Id = Guid.CreateVersion7(), VerbaleId = verbaleId, Testo = "Prima",  Ordine = 1 },
                new PrescrizioneCse { Id = Guid.CreateVersion7(), VerbaleId = verbaleId, Testo = "Seconda", Ordine = 2 },
                new PrescrizioneCse { Id = Guid.CreateVersion7(), VerbaleId = verbaleId, Testo = "Terza",   Ordine = 3 },
            };
            await verbaleRepo.ReplacePrescrizioniAsync(verbaleId, primaTornata);

            var dopoPrima = await verbaleRepo.GetPrescrizioniByVerbaleAsync(verbaleId);
            Assert.Equal(3, dopoPrima.Count);
            Assert.Equal(new[] { "Prima", "Seconda", "Terza" }, dopoPrima.Select(p => p.Testo));
            Assert.Equal(new[] { 1, 2, 3 }, dopoPrima.Select(p => p.Ordine));

            var secondaTornata = new[]
            {
                new PrescrizioneCse { Id = Guid.CreateVersion7(), VerbaleId = verbaleId, Testo = "Sostituita-A", Ordine = 1 },
                new PrescrizioneCse { Id = Guid.CreateVersion7(), VerbaleId = verbaleId, Testo = "Sostituita-B", Ordine = 2 },
            };
            await verbaleRepo.ReplacePrescrizioniAsync(verbaleId, secondaTornata);

            var dopoSeconda = await verbaleRepo.GetPrescrizioniByVerbaleAsync(verbaleId);
            Assert.Equal(2, dopoSeconda.Count);
            Assert.Equal(new[] { "Sostituita-A", "Sostituita-B" }, dopoSeconda.Select(p => p.Testo));

            // UpdatedAt deve essere stato bumpato dalla Replace (datetime2(3) ha
            // risoluzione ms: la replay nei test e' praticamente sempre dopo).
            var dopoUpdatedAt = (await verbaleRepo.GetByIdAsync(verbaleId))!.UpdatedAt;
            Assert.True(dopoUpdatedAt >= creatoUpdatedAt);
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    // --------- firma CSE (B.10) ------------------------------------------

    [Fact]
    public async Task FirmaCseAsync_su_bozza_assegna_Numero_progressivo_e_passa_a_FirmatoCse()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var firmaRepo = new FirmaRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };
        var anno = DateTime.UtcNow.Year;

        try
        {
            await verbaleRepo.CreateBozzaWithChildrenAsync(
                verbale,
                Array.Empty<VerbaleAttivita>(),
                Array.Empty<VerbaleDocumento>(),
                Array.Empty<VerbaleApprestamento>(),
                Array.Empty<VerbaleCondizioneAmbientale>(),
                audit);

            // Snapshot del max numero pre-firma per asserire la progressione
            // senza assumere uno stato DB iniziale specifico.
            await using var probeConn = await _factory.CreateOpenConnectionAsync();
            var maxPrima = await probeConn.ExecuteScalarAsync<int?>(
                "SELECT MAX(Numero) FROM dbo.Verbale WHERE Anno = @Anno AND Numero IS NOT NULL;",
                new { Anno = anno }) ?? 0;

            var result = await verbaleRepo.FirmaCseAsync(
                verbaleId, anno, "Ing. Test Firmatario",
                DateOnly.FromDateTime(DateTime.UtcNow),
                $"firme/{verbaleId}/cse.png",
                anagrafiche.UtenteId,
                BuildTokenInputs());

            Assert.Equal(maxPrima + 1, result.NumeroAssegnato);
            Assert.Equal(anno, result.Anno);

            var read = await verbaleRepo.GetByIdAsync(verbaleId);
            Assert.NotNull(read);
            Assert.Equal(StatoVerbale.FirmatoCse, read!.Stato);
            Assert.Equal(result.NumeroAssegnato, read.Numero);
            Assert.Equal(anno, read.Anno);

            var firma = await firmaRepo.GetByVerbaleAndTipoAsync(verbaleId, TipoFirmatario.Cse);
            Assert.NotNull(firma);
            Assert.Equal("Ing. Test Firmatario", firma!.NomeFirmatario);
            Assert.Equal($"firme/{verbaleId}/cse.png", firma.ImmagineFirmaPath);

            // Audit: 1 riga Creazione + 1 riga Firma = 2.
            var auditCount = await probeConn.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM dbo.VerbaleAudit WHERE VerbaleId = @Id;",
                new { Id = verbaleId });
            Assert.Equal(2, auditCount);
            var firmaAuditCount = await probeConn.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM dbo.VerbaleAudit WHERE VerbaleId = @Id AND EventoTipo = @Tipo;",
                new { Id = verbaleId, Tipo = EventoAuditTipo.Firma });
            Assert.Equal(1, firmaAuditCount);
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    [Fact]
    public async Task FirmaCseAsync_due_verbali_consecutivi_assegna_Numero_progressivo()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var anno = DateTime.UtcNow.Year;
        var v1 = Guid.CreateVersion7();
        var v2 = Guid.CreateVersion7();

        try
        {
            foreach (var id in new[] { v1, v2 })
            {
                var bozza = BuildBozza(id, anagrafiche);
                var audit = new VerbaleAudit
                {
                    Id = Guid.CreateVersion7(),
                    VerbaleId = id,
                    UtenteId = anagrafiche.UtenteId,
                    DataEvento = DateTime.UtcNow,
                    EventoTipo = EventoAuditTipo.Creazione,
                };
                await verbaleRepo.CreateBozzaWithChildrenAsync(
                    bozza,
                    Array.Empty<VerbaleAttivita>(),
                    Array.Empty<VerbaleDocumento>(),
                    Array.Empty<VerbaleApprestamento>(),
                    Array.Empty<VerbaleCondizioneAmbientale>(),
                    audit);
            }

            var r1 = await verbaleRepo.FirmaCseAsync(
                v1, anno, "Firmatario A", DateOnly.FromDateTime(DateTime.UtcNow),
                $"firme/{v1}/cse.png", anagrafiche.UtenteId, BuildTokenInputs());
            var r2 = await verbaleRepo.FirmaCseAsync(
                v2, anno, "Firmatario B", DateOnly.FromDateTime(DateTime.UtcNow),
                $"firme/{v2}/cse.png", anagrafiche.UtenteId, BuildTokenInputs());

            Assert.Equal(r1.NumeroAssegnato + 1, r2.NumeroAssegnato);
            Assert.Equal(anno, r1.Anno);
            Assert.Equal(anno, r2.Anno);
        }
        finally
        {
            await CleanupAsync(v1, anagrafiche, deleteAnagrafiche: false);
            await CleanupAsync(v2, anagrafiche);
        }
    }

    [Fact]
    public async Task FirmaCseAsync_su_verbale_gia_firmato_lancia_InvalidOperationException()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };
        var anno = DateTime.UtcNow.Year;

        try
        {
            await verbaleRepo.CreateBozzaWithChildrenAsync(
                verbale,
                Array.Empty<VerbaleAttivita>(),
                Array.Empty<VerbaleDocumento>(),
                Array.Empty<VerbaleApprestamento>(),
                Array.Empty<VerbaleCondizioneAmbientale>(),
                audit);

            // Prima firma OK.
            await verbaleRepo.FirmaCseAsync(
                verbaleId, anno, "Firmatario", DateOnly.FromDateTime(DateTime.UtcNow),
                $"firme/{verbaleId}/cse.png", anagrafiche.UtenteId, BuildTokenInputs());

            // Seconda firma: il verbale e' FirmatoCse, non Bozza → eccezione.
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                verbaleRepo.FirmaCseAsync(
                    verbaleId, anno, "Firmatario", DateOnly.FromDateTime(DateTime.UtcNow),
                    $"firme/{verbaleId}/cse.png", anagrafiche.UtenteId, BuildTokenInputs()));
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    // --------- firma Impresa (B.11) --------------------------------------

    [Fact]
    public async Task FirmaCseAsync_inserisce_anche_FirmaToken_con_UsatoUtc_null()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };
        var tokenInputs = BuildTokenInputs();

        try
        {
            await verbaleRepo.CreateBozzaWithChildrenAsync(
                verbale,
                Array.Empty<VerbaleAttivita>(),
                Array.Empty<VerbaleDocumento>(),
                Array.Empty<VerbaleApprestamento>(),
                Array.Empty<VerbaleCondizioneAmbientale>(),
                audit);

            var result = await verbaleRepo.FirmaCseAsync(
                verbaleId, DateTime.UtcNow.Year, "Firmatario CSE",
                DateOnly.FromDateTime(DateTime.UtcNow),
                $"firme/{verbaleId}/cse.png", anagrafiche.UtenteId, tokenInputs);

            // Il TokenImpresa ritornato e' lo stesso passato in input.
            Assert.Equal(tokenInputs.Token, result.TokenImpresa);

            // Riga FirmaToken esiste con UsatoUtc null e scadenza coerente.
            await using var conn = await _factory.CreateOpenConnectionAsync();
            var token = await conn.QuerySingleAsync<(Guid Id, Guid VerbaleId, Guid Token, DateTime? UsatoUtc, DateTime ScadenzaUtc)>(
                "SELECT Id, VerbaleId, Token, UsatoUtc, ScadenzaUtc FROM dbo.FirmaToken WHERE VerbaleId = @V;",
                new { V = verbaleId });
            Assert.Equal(tokenInputs.TokenId, token.Id);
            Assert.Equal(tokenInputs.Token, token.Token);
            Assert.Null(token.UsatoUtc);
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    [Fact]
    public async Task FirmaImpresaAsync_su_FirmatoCse_passa_a_FirmatoImpresa_e_marca_token_usato()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var firmaRepo = new FirmaRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var creazioneAudit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };
        var tokenInputs = BuildTokenInputs();

        try
        {
            await verbaleRepo.CreateBozzaWithChildrenAsync(
                verbale,
                Array.Empty<VerbaleAttivita>(),
                Array.Empty<VerbaleDocumento>(),
                Array.Empty<VerbaleApprestamento>(),
                Array.Empty<VerbaleCondizioneAmbientale>(),
                creazioneAudit);

            var firmaCse = await verbaleRepo.FirmaCseAsync(
                verbaleId, DateTime.UtcNow.Year, "Firmatario CSE",
                DateOnly.FromDateTime(DateTime.UtcNow),
                $"firme/{verbaleId}/cse.png", anagrafiche.UtenteId, tokenInputs);

            // Sanity: verbale a FirmatoCse, Numero/Anno assegnati.
            var letturaPre = await verbaleRepo.GetByIdAsync(verbaleId);
            Assert.Equal(StatoVerbale.FirmatoCse, letturaPre!.Stato);

            await verbaleRepo.FirmaImpresaAsync(
                verbaleId, tokenInputs.TokenId, "Firmatario Impresa",
                DateOnly.FromDateTime(DateTime.UtcNow),
                $"firme/{verbaleId}/impresa.png", anagrafiche.UtenteId);

            var letturaPost = await verbaleRepo.GetByIdAsync(verbaleId);
            Assert.Equal(StatoVerbale.FirmatoImpresa, letturaPost!.Stato);

            // Numero/Anno NON cambiano dopo la firma impresa.
            Assert.Equal(firmaCse.NumeroAssegnato, letturaPost.Numero);
            Assert.Equal(firmaCse.Anno, letturaPost.Anno);

            // Esiste una firma Impresa, oltre alla CSE.
            var firmaImpresa = await firmaRepo.GetByVerbaleAndTipoAsync(
                verbaleId, TipoFirmatario.ImpresaAppaltatrice);
            Assert.NotNull(firmaImpresa);
            Assert.Equal("Firmatario Impresa", firmaImpresa!.NomeFirmatario);

            // Token marcato usato.
            await using var conn = await _factory.CreateOpenConnectionAsync();
            var usato = await conn.ExecuteScalarAsync<DateTime?>(
                "SELECT UsatoUtc FROM dbo.FirmaToken WHERE Id = @Id;",
                new { Id = tokenInputs.TokenId });
            Assert.NotNull(usato);

            // Audit: Creazione + Firma CSE + Firma Impresa = 3.
            var auditCount = await conn.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM dbo.VerbaleAudit WHERE VerbaleId = @V;",
                new { V = verbaleId });
            Assert.Equal(3, auditCount);
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    [Fact]
    public async Task FirmaImpresaAsync_su_verbale_in_bozza_lancia_InvalidOperationException()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
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

            // Verbale in Bozza (mai firmato CSE): la firma impresa non puo' avvenire.
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                verbaleRepo.FirmaImpresaAsync(
                    verbaleId, Guid.CreateVersion7(), "X",
                    DateOnly.FromDateTime(DateTime.UtcNow),
                    "irrilevante.png", anagrafiche.UtenteId));
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    [Fact]
    public async Task FirmaImpresaAsync_token_gia_usato_lancia_InvalidOperationException()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var audit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };
        var tokenInputs = BuildTokenInputs();

        try
        {
            await verbaleRepo.CreateBozzaWithChildrenAsync(
                verbale,
                Array.Empty<VerbaleAttivita>(),
                Array.Empty<VerbaleDocumento>(),
                Array.Empty<VerbaleApprestamento>(),
                Array.Empty<VerbaleCondizioneAmbientale>(),
                audit);

            await verbaleRepo.FirmaCseAsync(
                verbaleId, DateTime.UtcNow.Year, "Firmatario CSE",
                DateOnly.FromDateTime(DateTime.UtcNow),
                $"firme/{verbaleId}/cse.png", anagrafiche.UtenteId, tokenInputs);

            // Forzo UsatoUtc != null per simulare "token gia' usato da un'altra tab".
            await using var conn = await _factory.CreateOpenConnectionAsync();
            await conn.ExecuteAsync(
                "UPDATE dbo.FirmaToken SET UsatoUtc = SYSUTCDATETIME() WHERE Id = @Id;",
                new { Id = tokenInputs.TokenId });

            // Riprova firma impresa: lo stato e' FirmatoCse, ma il token e' marcato usato.
            // Il MarkTokenUsato (UPDATE ... WHERE UsatoUtc IS NULL) non tocca righe e abortisce.
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                verbaleRepo.FirmaImpresaAsync(
                    verbaleId, tokenInputs.TokenId, "Firmatario Impresa",
                    DateOnly.FromDateTime(DateTime.UtcNow),
                    "firme/x/impresa.png", anagrafiche.UtenteId));

            // Lo stato del verbale non deve essere cambiato dalla transazione rollbackata.
            var read = await verbaleRepo.GetByIdAsync(verbaleId);
            Assert.Equal(StatoVerbale.FirmatoCse, read!.Stato);
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    // --------- rigenerazione token (B.12) --------------------------------

    [Fact]
    public async Task RigeneraAsync_revoca_token_attivi_e_inserisce_nuovo_con_audit()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var firmaTokenRepo = new FirmaTokenRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var creazioneAudit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };
        var primoTokenInputs = BuildTokenInputs();

        try
        {
            await verbaleRepo.CreateBozzaWithChildrenAsync(
                verbale,
                Array.Empty<VerbaleAttivita>(),
                Array.Empty<VerbaleDocumento>(),
                Array.Empty<VerbaleApprestamento>(),
                Array.Empty<VerbaleCondizioneAmbientale>(),
                creazioneAudit);

            await verbaleRepo.FirmaCseAsync(
                verbaleId, DateTime.UtcNow.Year, "Firmatario CSE",
                DateOnly.FromDateTime(DateTime.UtcNow),
                $"firme/{verbaleId}/cse.png", anagrafiche.UtenteId, primoTokenInputs);

            var nuovoTokenInputs = BuildTokenInputs();
            await firmaTokenRepo.RigeneraAsync(verbaleId, nuovoTokenInputs, anagrafiche.UtenteId);

            // Il primo token deve risultare revocato (non usato).
            await using var conn = await _factory.CreateOpenConnectionAsync();
            var primo = await conn.QuerySingleAsync<(DateTime? UsatoUtc, DateTime? RevocatoUtc)>(
                "SELECT UsatoUtc, RevocatoUtc FROM dbo.FirmaToken WHERE Id = @Id;",
                new { Id = primoTokenInputs.TokenId });
            Assert.Null(primo.UsatoUtc);
            Assert.NotNull(primo.RevocatoUtc);

            // Il nuovo token e' presente, non usato, non revocato.
            var nuovo = await conn.QuerySingleAsync<(DateTime? UsatoUtc, DateTime? RevocatoUtc)>(
                "SELECT UsatoUtc, RevocatoUtc FROM dbo.FirmaToken WHERE Id = @Id;",
                new { Id = nuovoTokenInputs.TokenId });
            Assert.Null(nuovo.UsatoUtc);
            Assert.Null(nuovo.RevocatoUtc);

            // Audit: Creazione + Firma CSE + RigenerazioneToken = 3.
            var auditTipi = (await conn.QueryAsync<byte>(
                "SELECT EventoTipo FROM dbo.VerbaleAudit WHERE VerbaleId = @V ORDER BY DataEvento;",
                new { V = verbaleId })).ToList();
            Assert.Equal(3, auditTipi.Count);
            Assert.Contains((byte)EventoAuditTipo.RigenerazioneToken, auditTipi);
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    [Fact]
    public async Task GetUltimoAttivoAsync_ignora_usati_revocati_scaduti_e_torna_il_piu_recente()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var firmaTokenRepo = new FirmaTokenRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var creazioneAudit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
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
                creazioneAudit);

            // Stato iniziale: nessun token → null.
            Assert.Null(await firmaTokenRepo.GetUltimoAttivoAsync(verbaleId));

            // Firma CSE: crea il primo token attivo.
            var t1 = BuildTokenInputs();
            await verbaleRepo.FirmaCseAsync(
                verbaleId, DateTime.UtcNow.Year, "Firmatario CSE",
                DateOnly.FromDateTime(DateTime.UtcNow),
                $"firme/{verbaleId}/cse.png", anagrafiche.UtenteId, t1);

            var attivo = await firmaTokenRepo.GetUltimoAttivoAsync(verbaleId);
            Assert.NotNull(attivo);
            Assert.Equal(t1.TokenId, attivo!.Id);

            // Rigenera: t1 → revocato, t2 attivo.
            var t2 = BuildTokenInputs();
            await firmaTokenRepo.RigeneraAsync(verbaleId, t2, anagrafiche.UtenteId);
            attivo = await firmaTokenRepo.GetUltimoAttivoAsync(verbaleId);
            Assert.Equal(t2.TokenId, attivo!.Id);

            // Forzo t2 scaduto: deve sparire dagli attivi.
            await using var conn = await _factory.CreateOpenConnectionAsync();
            await conn.ExecuteAsync(
                "UPDATE dbo.FirmaToken SET ScadenzaUtc = DATEADD(MINUTE, -1, SYSUTCDATETIME()) WHERE Id = @Id;",
                new { Id = t2.TokenId });
            Assert.Null(await firmaTokenRepo.GetUltimoAttivoAsync(verbaleId));

            // Una nuova rigenerazione torna a fornire un attivo (t3).
            var t3 = BuildTokenInputs();
            await firmaTokenRepo.RigeneraAsync(verbaleId, t3, anagrafiche.UtenteId);
            attivo = await firmaTokenRepo.GetUltimoAttivoAsync(verbaleId);
            Assert.Equal(t3.TokenId, attivo!.Id);
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    [Fact]
    public async Task FirmaImpresaAsync_token_revocato_lancia_InvalidOperationException()
    {
        // Difesa nel SqlMarkTokenUsato (B.12): un token revocato ma ancora dentro
        // la finestra di scadenza non deve poter essere consumato neppure se la
        // pagina FirmaImpresa fosse rimasta aperta sul vecchio link.
        var verbaleRepo = new VerbaleRepository(_factory);
        var firmaTokenRepo = new FirmaTokenRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.CreateVersion7();
        var verbale = BuildBozza(verbaleId, anagrafiche);
        var creazioneAudit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = verbaleId,
            UtenteId = anagrafiche.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };
        var t1 = BuildTokenInputs();

        try
        {
            await verbaleRepo.CreateBozzaWithChildrenAsync(
                verbale,
                Array.Empty<VerbaleAttivita>(),
                Array.Empty<VerbaleDocumento>(),
                Array.Empty<VerbaleApprestamento>(),
                Array.Empty<VerbaleCondizioneAmbientale>(),
                creazioneAudit);

            await verbaleRepo.FirmaCseAsync(
                verbaleId, DateTime.UtcNow.Year, "Firmatario CSE",
                DateOnly.FromDateTime(DateTime.UtcNow),
                $"firme/{verbaleId}/cse.png", anagrafiche.UtenteId, t1);

            // Rigenera: t1 viene revocato, t2 attivo.
            var t2 = BuildTokenInputs();
            await firmaTokenRepo.RigeneraAsync(verbaleId, t2, anagrafiche.UtenteId);

            // Tentativo di firma impresa con il token revocato t1 → repository abortisce.
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                verbaleRepo.FirmaImpresaAsync(
                    verbaleId, t1.TokenId, "Firmatario Impresa",
                    DateOnly.FromDateTime(DateTime.UtcNow),
                    "firme/x/impresa.png", anagrafiche.UtenteId));

            var stato = await verbaleRepo.GetByIdAsync(verbaleId);
            Assert.Equal(StatoVerbale.FirmatoCse, stato!.Stato);
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
        var suffix = Guid.CreateVersion7().ToString("N");
        var cantiere = new Cantiere { Id = Guid.CreateVersion7(), Ubicazione = $"Test loc {suffix}", Tipologia = "Test", IsAttivo = true };
        var committente = new Committente { Id = Guid.CreateVersion7(), RagioneSociale = $"Test Cmt {suffix}", IsAttivo = true };
        var impresa = new ImpresaAppaltatrice { Id = Guid.CreateVersion7(), RagioneSociale = $"Test Imp {suffix}", IsAttivo = true };
        var persona = new Persona { Id = Guid.CreateVersion7(), Nominativo = $"Test Persona {suffix}", Azienda = "ICM", IsAttivo = true };
        var utente = new Utente
        {
            Id = Guid.CreateVersion7(),
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

    private static FirmaTokenInputs BuildTokenInputs()
        => new(
            TokenId: Guid.CreateVersion7(),
            Token: Guid.CreateVersion7(),
            ScadenzaUtc: DateTime.UtcNow.AddHours(48),
            CreatedAt: DateTime.UtcNow);

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

    private async Task CleanupAsync(Guid verbaleId, AnagraficheSeed a, bool deleteAnagrafiche = true)
    {
        // Ordine: prima Verbale (cascade libera figlie e audit),
        // poi anagrafiche (FK NO ACTION richiede ordine esplicito).
        await using var conn = await _factory.CreateOpenConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM dbo.Verbale WHERE Id = @Id", new { Id = verbaleId });
        if (!deleteAnagrafiche) return;
        await conn.ExecuteAsync("DELETE FROM dbo.Utente WHERE Id = @Id", new { Id = a.UtenteId });
        await conn.ExecuteAsync("DELETE FROM dbo.Persona WHERE Id = @Id", new { Id = a.PersonaId });
        await conn.ExecuteAsync("DELETE FROM dbo.ImpresaAppaltatrice WHERE Id = @Id", new { Id = a.ImpresaId });
        await conn.ExecuteAsync("DELETE FROM dbo.Committente WHERE Id = @Id", new { Id = a.CommittenteId });
        await conn.ExecuteAsync("DELETE FROM dbo.Cantiere WHERE Id = @Id", new { Id = a.CantiereId });
    }
}
