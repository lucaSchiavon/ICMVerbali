using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Managers.Interfaces;

public interface ICantiereManager
{
    Task<Cantiere> CreaAsync(string ubicazione, string tipologia, decimal? importoAppalto, CancellationToken ct = default);
    Task<Cantiere?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Cantiere>> ListaAttiviAsync(CancellationToken ct = default);
}
