using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Managers;

public sealed class UtenteManager : IUtenteManager
{
    private readonly IUtenteRepository _repo;

    public UtenteManager(IUtenteRepository repo) => _repo = repo;

    public async Task<Utente> CreaAsync(string username, string? email, string passwordHash, RuoloUtente ruolo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username obbligatorio.", nameof(username));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash obbligatorio.", nameof(passwordHash));

        var now = DateTime.UtcNow;
        var utente = new Utente
        {
            Id = Guid.CreateVersion7(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            Ruolo = ruolo,
            IsAttivo = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        await _repo.CreateAsync(utente, ct);
        return utente;
    }

    public async Task AggiornaProfiloAsync(Utente utente, CancellationToken ct = default)
    {
        if (utente.Id == Guid.Empty)
            throw new ArgumentException("Id utente mancante.", nameof(utente));
        if (string.IsNullOrWhiteSpace(utente.Username))
            throw new ArgumentException("Username obbligatorio.");

        await _repo.UpdateProfileAsync(utente, ct);
    }

    public Task<Utente?> GetAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public Task<Utente?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => _repo.GetByUsernameAsync(username, ct);

    public Task<IReadOnlyList<Utente>> ListaAttiviAsync(CancellationToken ct = default)
        => _repo.GetAttiviAsync(ct);

    public Task<IReadOnlyList<Utente>> ListaTuttiAsync(CancellationToken ct = default)
        => _repo.GetAllAsync(ct);
}
