using Dapper;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Repositories;

namespace ICMVerbali.Tests.Repositories;

public class VerbaleRepositoryTests
{
    private readonly TestSqlConnectionFactory _factory = new();

    [Fact]
    public async Task Create_bozza_then_GetById_returns_record_with_null_numero_anno()
    {
        var verbaleRepo = new VerbaleRepository(_factory);
        var cantiereRepo = new CantiereRepository(_factory);
        var committenteRepo = new CommittenteRepository(_factory);
        var impresaRepo = new ImpresaAppaltatriceRepository(_factory);
        var personaRepo = new PersonaRepository(_factory);
        var utenteRepo = new UtenteRepository(_factory);

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

        var oggi = DateOnly.FromDateTime(DateTime.UtcNow);
        var verbale = new Verbale
        {
            Id = Guid.NewGuid(),
            Numero = null,
            Anno = null,
            Data = oggi,
            CantiereId = cantiere.Id,
            CommittenteId = committente.Id,
            ImpresaAppaltatriceId = impresa.Id,
            RlPersonaId = persona.Id,
            CspPersonaId = persona.Id,
            CsePersonaId = persona.Id,
            DlPersonaId = persona.Id,
            Esito = null,
            Meteo = null,
            TemperaturaCelsius = null,
            Interferenze = null,
            InterferenzeNote = null,
            Stato = StatoVerbale.Bozza,
            CompilatoDaUtenteId = utente.Id,
            IsDeleted = false,
            DeletedAt = null,
        };

        try
        {
            await cantiereRepo.CreateAsync(cantiere);
            await committenteRepo.CreateAsync(committente);
            await impresaRepo.CreateAsync(impresa);
            await personaRepo.CreateAsync(persona);
            await utenteRepo.CreateAsync(utente);
            await verbaleRepo.CreateAsync(verbale);

            var read = await verbaleRepo.GetByIdAsync(verbale.Id);

            Assert.NotNull(read);
            Assert.Equal(verbale.Id, read!.Id);
            Assert.Null(read.Numero);
            Assert.Null(read.Anno);
            Assert.Equal(oggi, read.Data);
            Assert.Equal(cantiere.Id, read.CantiereId);
            Assert.Equal(persona.Id, read.RlPersonaId);
            Assert.Equal(persona.Id, read.CspPersonaId);
            Assert.Null(read.Esito);
            Assert.Null(read.Meteo);
            Assert.Equal(StatoVerbale.Bozza, read.Stato);
            Assert.False(read.IsDeleted);
            Assert.Equal(utente.Id, read.CompilatoDaUtenteId);
        }
        finally
        {
            // Ordine cleanup: prima Verbale (cascade libera figlie inesistenti),
            // poi anagrafiche (FK NO ACTION richiede ordine esplicito).
            await using var conn = await _factory.CreateOpenConnectionAsync();
            await conn.ExecuteAsync("DELETE FROM dbo.Verbale WHERE Id = @Id", new { verbale.Id });
            await conn.ExecuteAsync("DELETE FROM dbo.Utente WHERE Id = @Id", new { utente.Id });
            await conn.ExecuteAsync("DELETE FROM dbo.Persona WHERE Id = @Id", new { persona.Id });
            await conn.ExecuteAsync("DELETE FROM dbo.ImpresaAppaltatrice WHERE Id = @Id", new { impresa.Id });
            await conn.ExecuteAsync("DELETE FROM dbo.Committente WHERE Id = @Id", new { committente.Id });
            await conn.ExecuteAsync("DELETE FROM dbo.Cantiere WHERE Id = @Id", new { cantiere.Id });
        }
    }
}
