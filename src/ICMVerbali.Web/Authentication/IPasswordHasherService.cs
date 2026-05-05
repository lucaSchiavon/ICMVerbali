namespace ICMVerbali.Web.Authentication;

public interface IPasswordHasherService
{
    string HashPassword(string password);
    bool VerifyHashedPassword(string hashedPassword, string providedPassword);
}
