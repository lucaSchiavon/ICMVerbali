namespace ICMVerbali.Web.Authentication;

// Centralizzazione dei nomi delle policy di autorizzazione (CLAUDE.md §5).
// La default policy "RequireAuthenticatedUser" e' applicata da [Authorize] senza
// specifiche e non necessita costante.
public static class AuthorizationPolicies
{
    public const string RequireAdmin = "RequireAdmin";
}
