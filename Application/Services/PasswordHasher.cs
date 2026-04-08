using System.Security.Cryptography;
using System.Text;
using Application.Common.Security;
using Application.Interfaces;
using Application.Options;

namespace Application.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private readonly AuthOptions _options;

        public PasswordHasher(AuthOptions options)
        {
            _options = options;
        }

        public string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                _options.PasswordHashIterations,
                HashAlgorithmName.SHA256,
                32);

            return $"PBKDF2$SHA256${_options.PasswordHashIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public PasswordVerificationStatus VerifyPassword(string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword))
            {
                return PasswordVerificationStatus.Failed;
            }

            if (!hashedPassword.StartsWith("PBKDF2$", StringComparison.Ordinal))
            {
                using var sha256 = SHA256.Create();
                var legacyHash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(providedPassword)));
                return string.Equals(legacyHash, hashedPassword, StringComparison.OrdinalIgnoreCase)
                    ? PasswordVerificationStatus.SuccessRehashNeeded
                    : PasswordVerificationStatus.Failed;
            }

            var parts = hashedPassword.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5 || !int.TryParse(parts[2], out var iterations))
            {
                return PasswordVerificationStatus.Failed;
            }

            var salt = Convert.FromBase64String(parts[3]);
            var expectedHash = Convert.FromBase64String(parts[4]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                providedPassword,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            if (!CryptographicOperations.FixedTimeEquals(expectedHash, actualHash))
            {
                return PasswordVerificationStatus.Failed;
            }

            return iterations < _options.PasswordHashIterations
                ? PasswordVerificationStatus.SuccessRehashNeeded
                : PasswordVerificationStatus.Success;
        }
    }
}
