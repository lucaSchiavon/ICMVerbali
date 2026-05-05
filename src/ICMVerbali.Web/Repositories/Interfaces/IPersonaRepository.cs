using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Repositories.Interfaces;

public interface IPersonaRepository
{
    Task CreateAsync(Persona persona, CancellationToken ct = default);
    Task<Persona?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Persona>> GetAttiveAsync(CancellationToken ct = default);
}
