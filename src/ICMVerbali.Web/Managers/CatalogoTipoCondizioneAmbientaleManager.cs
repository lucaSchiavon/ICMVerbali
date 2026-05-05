using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Managers.Interfaces;
using ICMVerbali.Web.Repositories.Interfaces;

namespace ICMVerbali.Web.Managers;

public sealed class CatalogoTipoCondizioneAmbientaleManager : ICatalogoTipoCondizioneAmbientaleManager
{
    private readonly ICatalogoTipoCondizioneAmbientaleRepository _repo;

    public CatalogoTipoCondizioneAmbientaleManager(ICatalogoTipoCondizioneAmbientaleRepository repo) => _repo = repo;

    public Task<IReadOnlyList<CatalogoTipoCondizioneAmbientale>> ListaAttiviAsync(CancellationToken ct = default)
        => _repo.GetAllAttiviAsync(ct);

    public Task<CatalogoTipoCondizioneAmbientale?> GetAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);
}
