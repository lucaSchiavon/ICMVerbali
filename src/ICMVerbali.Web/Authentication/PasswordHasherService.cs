using ICMVerbali.Web.Entities;
using Microsoft.AspNetCore.Identity;

namespace ICMVerbali.Web.Authentication;

// Wrapper sul PasswordHasher<TUser> di ASP.NET Core Identity (PBKDF2-SHA256
// 100k iterazioni, salt 128bit, formato standard v3). Vedi docs/01-design.md §4.2.
//
// PasswordHasher<TUser> non usa nulla del TUser: la generic e' solo compile-time.
// Passiamo un'istanza dummy di Utente per soddisfare la firma.
public sealed class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<Utente> _hasher = new();
    private static readonly Utente _dummy = new();

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password obbligatoria.", nameof(password));
        return _hasher.HashPassword(_dummy, password);
    }

    public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
            return false;
        var result = _hasher.VerifyHashedPassword(_dummy, hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
