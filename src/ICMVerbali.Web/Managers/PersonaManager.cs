using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Managers;

public sealed class PersonaManager : IPersonaManager
{
    private readonly IPersonaRepository _repo;

    public PersonaManager(IPersonaRepository repo) => _repo = repo;

    public async Task<Persona> CreaAsync(string nominativo, string azienda, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nominativo))
            throw new ArgumentException("Nominativo obbligatorio.", nameof(nominativo));
        if (string.IsNullOrWhiteSpace(azienda))
            throw new ArgumentException("Azienda obbligatoria.", nameof(azienda));

        var persona = new Persona
        {
            Id = Guid.CreateVersion7(),
            Nominativo = nominativo,
            Azienda = azienda,
            IsAttivo = true,
        };
        await _repo.CreateAsync(persona, ct);
        return persona;
    }

    public async Task AggiornaAsync(Persona persona, CancellationToken ct = default)
    {
        if (persona.Id == Guid.Empty)
            throw new ArgumentException("Id persona mancante.", nameof(persona));
        if (string.IsNullOrWhiteSpace(persona.Nominativo))
            throw new ArgumentException("Nominativo obbligatorio.");
        if (string.IsNullOrWhiteSpace(persona.Azienda))
            throw new ArgumentException("Azienda obbligatoria.");

        await _repo.UpdateAsync(persona, ct);
    }

    public Task<Persona?> GetAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<Persona>> ListaAttiveAsync(CancellationToken ct = default)
        => _repo.GetAttiveAsync(ct);

    public Task<IReadOnlyList<Persona>> ListaTutteAsync(CancellationToken ct = default)
        => _repo.GetAllAsync(ct);
}
