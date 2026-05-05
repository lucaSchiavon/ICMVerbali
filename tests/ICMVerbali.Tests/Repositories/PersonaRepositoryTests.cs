using Dapper;
using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Repositories;

namespace ICMVerbali.Tests.Repositories;

public class PersonaRepositoryTests
{
    private readonly TestSqlConnectionFactory _factory = new();

    [Fact]
    public async Task Create_then_GetById_returns_same_record()
    {
        var repo = new PersonaRepository(_factory);
        var persona = new Persona
        {
            Id = Guid.NewGuid(),
            Nominativo = $"Ing. Test {Guid.NewGuid():N}",
            Azienda = "ICM Solutions",
            IsAttivo = true,
        };

        try
        {
            await repo.CreateAsync(persona);
            var read = await repo.GetByIdAsync(persona.Id);

            Assert.NotNull(read);
            Assert.Equal(persona.Id, read!.Id);
            Assert.Equal(persona.Nominativo, read.Nominativo);
            Assert.Equal(persona.Azienda, read.Azienda);
            Assert.True(read.IsAttivo);
        }
        finally
        {
            await using var conn = await _factory.CreateOpenConnectionAsync();
            await conn.ExecuteAsync("DELETE FROM dbo.Persona WHERE Id = @Id", new { persona.Id });
        }
    }
}
