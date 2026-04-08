using Application.Common.Security;

namespace Application.Interfaces
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        PasswordVerificationStatus VerifyPassword(string hashedPassword, string providedPassword);
    }
}
