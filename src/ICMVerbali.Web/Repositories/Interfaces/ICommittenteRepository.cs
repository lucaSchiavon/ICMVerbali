using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Repositories.Interfaces;

public interface ICommittenteRepository
{
    Task CreateAsync(Committente committente, CancellationToken ct = default);
    Task<Committente?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Committente>> GetAttiviAsync(CancellationToken ct = default);
}
