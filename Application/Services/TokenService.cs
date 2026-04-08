using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Common.Security;
using Application.Interfaces;
using Application.Options;
using Domain.Entities;

namespace Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly byte[] _signingKeyBytes;

        public TokenService(JwtOptions options, TimeProvider timeProvider)
        {
            _options = options;
            _timeProvider = timeProvider;
            _signingKeyBytes = Encoding.UTF8.GetBytes(_options.SigningKey);
        }

        public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(User user)
        {
            var now = _timeProvider.GetUtcNow();
            var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

            var header = new Dictionary<string, object?>
            {
                ["alg"] = "HS256",
                ["typ"] = "JWT"
            };

            var payload = new Dictionary<string, object?>
            {
                ["iss"] = _options.Issuer,
                ["aud"] = _options.Audience,
                ["sub"] = user.Id.ToString(CultureInfo.InvariantCulture),
                ["email"] = user.Email,
                ["name"] = user.UserName,
                ["role"] = user.Role.ToString(),
                [JwtClaimNames.SecurityStamp] = user.SecurityStamp,
                [JwtClaimNames.JwtId] = Guid.NewGuid().ToString("N"),
                ["iat"] = now.ToUnixTimeSeconds(),
                ["nbf"] = now.ToUnixTimeSeconds(),
                ["exp"] = expiresAt.ToUnixTimeSeconds()
            };

            var encodedHeader = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header)));
            var encodedPayload = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
            var unsignedToken = $"{encodedHeader}.{encodedPayload}";

            using var hmac = new HMACSHA256(_signingKeyBytes);
            var signature = Base64UrlEncoder.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken)));

            return ($"{unsignedToken}.{signature}", expiresAt.UtcDateTime);
        }

        public (string Token, string TokenHash, DateTime ExpiresAtUtc) CreateRefreshToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var token = Base64UrlEncoder.Encode(tokenBytes);
            var expiresAt = _timeProvider.GetUtcNow().AddDays(_options.RefreshTokenDays).UtcDateTime;

            return (token, HashOpaqueToken(token), expiresAt);
        }

        public string HashOpaqueToken(string token)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(token)));
        }

        public TokenValidationResult ValidateAccessToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new TokenValidationResult
                {
                    FailureReason = "Token is missing."
                };
            }

            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return new TokenValidationResult
                {
                    FailureReason = "Token format is invalid."
                };
            }

            try
            {
                var unsignedToken = $"{parts[0]}.{parts[1]}";
                using var hmac = new HMACSHA256(_signingKeyBytes);
                var expectedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken));
                var actualSignature = Base64UrlEncoder.Decode(parts[2]);

                if (!CryptographicOperations.FixedTimeEquals(expectedSignature, actualSignature))
                {
                    return new TokenValidationResult
                    {
                        FailureReason = "Token signature is invalid."
                    };
                }

                using var header = JsonDocument.Parse(Base64UrlEncoder.Decode(parts[0]));
                var alg = header.RootElement.GetProperty("alg").GetString();
                if (!string.Equals(alg, "HS256", StringComparison.Ordinal))
                {
                    return new TokenValidationResult
                    {
                        FailureReason = "Token algorithm is invalid."
                    };
                }

                using var payload = JsonDocument.Parse(Base64UrlEncoder.Decode(parts[1]));
                var root = payload.RootElement;
                var issuer = root.GetProperty("iss").GetString();
                var audience = root.GetProperty("aud").GetString();

                if (!string.Equals(issuer, _options.Issuer, StringComparison.Ordinal)
                    || !string.Equals(audience, _options.Audience, StringComparison.Ordinal))
                {
                    return new TokenValidationResult
                    {
                        FailureReason = "Token issuer or audience is invalid."
                    };
                }

                var now = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
                var notBefore = root.GetProperty("nbf").GetInt64();
                var expiresAt = root.GetProperty("exp").GetInt64();
                if (now < notBefore || now >= expiresAt)
                {
                    return new TokenValidationResult
                    {
                        FailureReason = "Token has expired or is not active yet."
                    };
                }

                var subject = root.GetProperty("sub").GetString();
                var email = root.GetProperty("email").GetString();
                var name = root.GetProperty("name").GetString();
                var role = root.GetProperty("role").GetString();
                var securityStamp = root.GetProperty(JwtClaimNames.SecurityStamp).GetString();

                if (!int.TryParse(subject, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
                {
                    return new TokenValidationResult
                    {
                        FailureReason = "Token subject is invalid."
                    };
                }

                return new TokenValidationResult
                {
                    Succeeded = true,
                    UserId = userId,
                    SecurityStamp = securityStamp,
                    Claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, subject ?? string.Empty),
                        new Claim(ClaimTypes.Email, email ?? string.Empty),
                        new Claim(ClaimTypes.Name, name ?? string.Empty),
                        new Claim(ClaimTypes.Role, role ?? string.Empty),
                        new Claim(JwtClaimNames.SecurityStamp, securityStamp ?? string.Empty)
                    }
                };
            }
            catch
            {
                return new TokenValidationResult
                {
                    FailureReason = "Token payload is invalid."
                };
            }
        }
    }
}
