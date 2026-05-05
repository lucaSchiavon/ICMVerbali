using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Repositories.Interfaces;

public interface IUtenteRepository
{
    Task CreateAsync(Utente utente, CancellationToken ct = default);
    Task UpdateProfileAsync(Utente utente, CancellationToken ct = default);
    Task<Utente?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Utente?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<Utente>> GetAttiviAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Utente>> GetAllAsync(CancellationToken ct = default);
}
