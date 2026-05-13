using Dapper;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Repositories;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Tests.Repositories;

public class FotoRepositoryTests
{
    private readonly TestSqlConnectionFactory _factory = new();

    [Fact]
    public async Task Create_then_GetByVerbale_returns_ordered_and_supports_didascalia_roundtrip()
    {
        var fotoRepo = new FotoRepository(_factory);
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.CreateVersion7();

        try
        {
            await CreateBozzaAsync(verbaleRepo, verbaleId, anagrafiche);

            // Create assigna Ordine come max+1: 3 INSERT consecutivi -> 1,2,3.
            var f1 = new Foto { Id = Guid.CreateVersion7(), VerbaleId = verbaleId, FilePathRelativo = "verbali/" + verbaleId + "/a.jpg" };
            var f2 = new Foto { Id = Guid.CreateVersion7(), VerbaleId = verbaleId, FilePathRelativo = "verbali/" + verbaleId + "/b.jpg" };
            var f3 = new Foto { Id = Guid.CreateVersion7(), VerbaleId = verbaleId, FilePathRelativo = "verbali/" + verbaleId + "/c.jpg" };
            await fotoRepo.CreateAsync(f1);
            await fotoRepo.CreateAsync(f2);
            await fotoRepo.CreateAsync(f3);

            var read = await fotoRepo.GetByVerbaleAsync(verbaleId);
            Assert.Equal(3, read.Count);
            Assert.Equal(new[] { 1, 2, 3 }, read.Select(f => f.Ordine));
            Assert.Equal(new[] { f1.Id, f2.Id, f3.Id }, read.Select(f => f.Id));

            await fotoRepo.UpdateDidascaliaAsync(f2.Id, "Trabattello cantiere ovest");
            var f2Read = await fotoRepo.GetByIdAsync(f2.Id);
            Assert.NotNull(f2Read);
            Assert.Equal("Trabattello cantiere ovest", f2Read!.Didascalia);
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    [Fact]
    public async Task Delete_returns_filepath_and_removes_row()
    {
        var fotoRepo = new FotoRepository(_factory);
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.CreateVersion7();

        try
        {
            await CreateBozzaAsync(verbaleRepo, verbaleId, anagrafiche);
            var f1 = new Foto { Id = Guid.CreateVersion7(), VerbaleId = verbaleId, FilePathRelativo = "verbali/" + verbaleId + "/x.jpg" };
            await fotoRepo.CreateAsync(f1);

            var path = await fotoRepo.DeleteAsync(f1.Id);
            Assert.Equal("verbali/" + verbaleId + "/x.jpg", path);

            var stillThere = await fotoRepo.GetByIdAsync(f1.Id);
            Assert.Null(stillThere);

            // Idempotenza: secondo delete restituisce null senza errore.
            var second = await fotoRepo.DeleteAsync(f1.Id);
            Assert.Null(second);
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    [Fact]
    public async Task UpdateOrdineBulk_persists_new_ordering()
    {
        var fotoRepo = new FotoRepository(_factory);
        var verbaleRepo = new VerbaleRepository(_factory);
        var anagrafiche = await SeedAnagraficheAsync();
        var verbaleId = Guid.CreateVersion7();

        try
        {
            await CreateBozzaAsync(verbaleRepo, verbaleId, anagrafiche);
            var ids = new[] { Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7() };
            foreach (var id in ids)
            {
                await fotoRepo.CreateAsync(new Foto
                {
                    Id = id,
                    VerbaleId = verbaleId,
                    FilePathRelativo = $"verbali/{verbaleId}/{id:N}.jpg",
                });
            }

            // Riordina: 3-1-2.
            var updates = new[]
            {
                new FotoOrdineUpdate(ids[2], 1),
                new FotoOrdineUpdate(ids[0], 2),
                new FotoOrdineUpdate(ids[1], 3),
            };
            await fotoRepo.UpdateOrdineBulkAsync(verbaleId, updates);

            var read = await fotoRepo.GetByVerbaleAsync(verbaleId);
            Assert.Equal(new[] { ids[2], ids[0], ids[1] }, read.Select(f => f.Id));
            Assert.Equal(new[] { 1, 2, 3 }, read.Select(f => f.Ordine));
        }
        finally
        {
            await CleanupAsync(verbaleId, anagrafiche);
        }
    }

    // ---------- helpers -------------------------------------------------

    private sealed record AnagraficheSeed(
        Guid CantiereId, Guid CommittenteId, Guid ImpresaId, Guid PersonaId, Guid UtenteId);

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

        return new AnagraficheSeed(cantiere.Id, committente.Id, impresa.Id, persona.Id, utente.Id);
    }

    private static async Task CreateBozzaAsync(IVerbaleRepository repo, Guid verbaleId, AnagraficheSeed a)
    {
        var verbale = new Verbale
        {
            Id = verbaleId,
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
            Stato = StatoVerbale.Bozza,
            CompilatoDaUtenteId = a.UtenteId,
            IsDeleted = false,
        };
        var audit = new VerbaleAudit
        {
            Id = Guid.CreateVersion7(),
            VerbaleId = verbaleId,
            UtenteId = a.UtenteId,
            DataEvento = DateTime.UtcNow,
            EventoTipo = EventoAuditTipo.Creazione,
        };
        await repo.CreateBozzaWithChildrenAsync(
            verbale,
            Array.Empty<VerbaleAttivita>(),
            Array.Empty<VerbaleDocumento>(),
            Array.Empty<VerbaleApprestamento>(),
            Array.Empty<VerbaleCondizioneAmbientale>(),
            audit);
    }

    private async Task CleanupAsync(Guid verbaleId, AnagraficheSeed a)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM dbo.Verbale WHERE Id = @Id", new { Id = verbaleId });
        await conn.ExecuteAsync("DELETE FROM dbo.Utente WHERE Id = @Id", new { Id = a.UtenteId });
        await conn.ExecuteAsync("DELETE FROM dbo.Persona WHERE Id = @Id", new { Id = a.PersonaId });
        await conn.ExecuteAsync("DELETE FROM dbo.ImpresaAppaltatrice WHERE Id = @Id", new { Id = a.ImpresaId });
        await conn.ExecuteAsync("DELETE FROM dbo.Committente WHERE Id = @Id", new { Id = a.CommittenteId });
        await conn.ExecuteAsync("DELETE FROM dbo.Cantiere WHERE Id = @Id", new { Id = a.CantiereId });
    }
}
