using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Repositories.Interfaces;

public interface IImpresaAppaltatriceRepository
{
    Task CreateAsync(ImpresaAppaltatrice impresa, CancellationToken ct = default);
    Task UpdateAsync(ImpresaAppaltatrice impresa, CancellationToken ct = default);
    Task<ImpresaAppaltatrice?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ImpresaAppaltatrice>> GetAttiviAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ImpresaAppaltatrice>> GetAllAsync(CancellationToken ct = default);
}
