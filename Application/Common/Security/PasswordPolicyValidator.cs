using Application.Common.Exceptions;
using Application.Options;

namespace Application.Common.Security
{
    public static class PasswordPolicyValidator
    {
        public static void Validate(string password, AuthOptions options)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ValidationException("Password is required.");
            }

            if (password.Length < options.MinPasswordLength)
            {
                throw new ValidationException($"Password must be at least {options.MinPasswordLength} characters long.");
            }

            if (options.RequireUppercase && !password.Any(char.IsUpper))
            {
                throw new ValidationException("Password must contain at least one uppercase letter.");
            }

            if (options.RequireLowercase && !password.Any(char.IsLower))
            {
                throw new ValidationException("Password must contain at least one lowercase letter.");
            }

            if (options.RequireDigit && !password.Any(char.IsDigit))
            {
                throw new ValidationException("Password must contain at least one digit.");
            }

            if (options.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
            {
                throw new ValidationException("Password must contain at least one non-alphanumeric character.");
            }
        }
    }
}
