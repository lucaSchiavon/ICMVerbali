using ICMVerbali.Web.Entities;

namespace ICMVerbali.Web.Managers.Interfaces;

public interface IPersonaManager
{
    Task<Persona> CreaAsync(string nominativo, string azienda, CancellationToken ct = default);
    Task<Persona?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Persona>> ListaAttiveAsync(CancellationToken ct = default);
}
