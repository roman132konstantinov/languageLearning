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
                throw new ValidationException("Введите пароль.");
            }

            if (password.Length < options.MinPasswordLength)
            {
                throw new ValidationException($"Пароль должен содержать минимум {options.MinPasswordLength} символов.");
            }

            if (options.RequireUppercase && !password.Any(char.IsUpper))
            {
                throw new ValidationException("Пароль должен содержать хотя бы одну заглавную букву.");
            }

            if (options.RequireLowercase && !password.Any(char.IsLower))
            {
                throw new ValidationException("Пароль должен содержать хотя бы одну строчную букву.");
            }

            if (options.RequireDigit && !password.Any(char.IsDigit))
            {
                throw new ValidationException("Пароль должен содержать хотя бы одну цифру.");
            }

            if (options.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
            {
                throw new ValidationException("Пароль должен содержать хотя бы один специальный символ.");
            }
        }
    }
}
