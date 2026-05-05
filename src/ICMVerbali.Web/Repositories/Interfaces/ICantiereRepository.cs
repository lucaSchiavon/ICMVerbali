using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Repositories.Interfaces;

public interface ICantiereRepository
{
    Task CreateAsync(Cantiere cantiere, CancellationToken ct = default);
    Task UpdateAsync(Cantiere cantiere, CancellationToken ct = default);
    Task<Cantiere?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Cantiere>> GetAttiviAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Cantiere>> GetAllAsync(CancellationToken ct = default);
}
