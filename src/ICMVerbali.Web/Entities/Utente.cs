using ICMVerbali.Web.Entities.Enums;

namespace ICMVerbali.Web.Entities;

public sealed class Utente
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public RuoloUtente Ruolo { get; set; }
    public bool IsAttivo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
