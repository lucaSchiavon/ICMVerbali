using ICMVerbali.Web.Entities;
using ICMVerbali.Web.Entities.Enums;

namespace ICMVerbali.Web.Managers.Interfaces;

public interface IUtenteManager
{
    // CreaAsync prende la password gia' hashata: l'hashing vive nel servizio
    // dedicato della fase B.6 (PBKDF2 via PasswordHasher<Utente>).
    Task<Utente> CreaAsync(string username, string? email, string passwordHash, RuoloUtente ruolo, CancellationToken ct = default);
    Task<Utente?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Utente?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<Utente>> ListaAttiviAsync(CancellationToken ct = default);
}
