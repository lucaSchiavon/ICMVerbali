using Dapper;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Repositories;

namespace ICMVerbali.Tests.Repositories;

public class UtenteRepositoryTests
{
    private readonly TestSqlConnectionFactory _factory = new();

    [Fact]
    public async Task Create_then_GetById_returns_same_record()
    {
        var repo = new UtenteRepository(_factory);
        var now = DateTime.UtcNow;
        var utente = new Utente
        {
            Id = Guid.CreateVersion7(),
            Username = $"test-user-{Guid.CreateVersion7():N}",
            Email = $"test-{Guid.CreateVersion7():N}@example.com",
            PasswordHash = "fake-hash-not-real-pbkdf2",
            Ruolo = RuoloUtente.Cse,
            IsAttivo = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        try
        {
            await repo.CreateAsync(utente);
            var read = await repo.GetByIdAsync(utente.Id);

            Assert.NotNull(read);
            Assert.Equal(utente.Id, read!.Id);
            Assert.Equal(utente.Username, read.Username);
            Assert.Equal(utente.Email, read.Email);
            Assert.Equal(utente.PasswordHash, read.PasswordHash);
            Assert.Equal(RuoloUtente.Cse, read.Ruolo);
            Assert.True(read.IsAttivo);
        }
        finally
        {
            await using var conn = await _factory.CreateOpenConnectionAsync();
            await conn.ExecuteAsync("DELETE FROM dbo.Utente WHERE Id = @Id", new { utente.Id });
        }
    }
}
