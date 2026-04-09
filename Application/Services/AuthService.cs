using Application.Common.Diagnostics;
using Application.Common.Exceptions;
using Application.Common.Mappings;
using Application.Common.Security;
using Application.DTOs.Auth;
using Application.DTOs.Users;
using Application.Interfaces;
using Application.Options;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly AuthOptions _authOptions;
        private readonly TimeProvider _timeProvider;

        public AuthService(
            IAuthRepository authRepository,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            AuthOptions authOptions,
            TimeProvider timeProvider)
        {
            _authRepository = authRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _authOptions = authOptions;
            _timeProvider = timeProvider;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, AuthRequestMetadata metadata)
        {
            ValidateRegisterDto(dto);

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            if (await _authRepository.GetUserByEmailAsync(normalizedEmail) is not null)
            {
                await WriteAuditAsync(null, normalizedEmail, AuthAuditEventType.Register, false, metadata, "Email already exists.");
                await _authRepository.SaveChangesAsync();
                throw new ConflictException("User with this email already exists.");
            }

            var role = _authOptions.BootstrapFirstUserAsAdmin && !await _authRepository.AnyUsersAsync()
                ? UserRole.Admin
                : UserRole.User;

            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                UserName = dto.UserName.Trim(),
                Role = role,
                Level = dto.Level,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                IsActive = true,
                EmailConfirmed = false,
                SecurityStamp = Guid.NewGuid().ToString("N")
            };

            await _authRepository.AddUserAsync(user);
            await _authRepository.SaveChangesAsync();
            var response = await CreateSessionAsync(user, metadata, AuthAuditEventType.Register, "Registered successfully.");
            await _authRepository.SaveChangesAsync();

            return response;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, AuthRequestMetadata metadata)
        {
            ValidateLoginDto(dto);

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var user = await _authRepository.GetUserByEmailAsync(normalizedEmail);
            if (user is null)
            {
                await WriteAuditAsync(null, normalizedEmail, AuthAuditEventType.Login, false, metadata, "Invalid credentials.");
                await _authRepository.SaveChangesAsync();
                throw new UnauthorizedException("Неверный email или пароль.");
            }

            EnsureUserCanAuthenticate(user);

            var verificationStatus = _passwordHasher.VerifyPassword(user.PasswordHash, dto.Password);
            if (verificationStatus == PasswordVerificationStatus.Failed)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= _authOptions.MaxFailedAccessAttempts)
                {
                    user.LockoutEndUtc = _timeProvider.GetUtcNow().AddMinutes(_authOptions.LockoutMinutes).UtcDateTime;
                    user.FailedLoginAttempts = 0;
                }

                _authRepository.UpdateUser(user);
                await WriteAuditAsync(user, normalizedEmail, AuthAuditEventType.Login, false, metadata, "Invalid credentials.");
                await _authRepository.SaveChangesAsync();
                throw new UnauthorizedException("Неверный email или пароль.");
            }

            if (verificationStatus == PasswordVerificationStatus.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(dto.Password);
            }

            user.FailedLoginAttempts = 0;
            user.LockoutEndUtc = null;
            user.LastLoginAt = _timeProvider.GetUtcNow().UtcDateTime;
            _authRepository.UpdateUser(user);

            var response = await CreateSessionAsync(user, metadata, AuthAuditEventType.Login, "Authenticated successfully.");
            await _authRepository.SaveChangesAsync();

            return response;
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto, AuthRequestMetadata metadata)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                throw new ValidationException("Refresh token is required.");
            }

            var hashedToken = _tokenService.HashOpaqueToken(dto.RefreshToken);
            var currentToken = await _authRepository.GetRefreshTokenByHashWithUserAsync(hashedToken);
            if (currentToken?.User is null)
            {
                await WriteAuditAsync(null, null, AuthAuditEventType.Refresh, false, metadata, "Refresh token was not found.");
                await _authRepository.SaveChangesAsync();
                throw new UnauthorizedException("Refresh token is invalid.");
            }

            EnsureUserCanAuthenticate(currentToken.User);

            if (currentToken.RevokedAt is not null || currentToken.ExpiresAt <= _timeProvider.GetUtcNow().UtcDateTime)
            {
                await WriteAuditAsync(currentToken.User, currentToken.User.Email, AuthAuditEventType.Refresh, false, metadata, "Refresh token is inactive.");
                await _authRepository.SaveChangesAsync();
                throw new UnauthorizedException("Refresh token is invalid.");
            }

            var refreshedResponse = await RotateRefreshTokenAsync(currentToken, metadata);
            await _authRepository.SaveChangesAsync();

            return refreshedResponse;
        }

        public async Task LogoutAsync(int userId, LogoutRequestDto dto, AuthRequestMetadata metadata)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                throw new ValidationException("Refresh token is required.");
            }

            var user = await _authRepository.GetUserByIdWithRefreshTokensAsync(userId)
                ?? throw new NotFoundException("User not found.");

            var tokenHash = _tokenService.HashOpaqueToken(dto.RefreshToken);
            var refreshToken = user.RefreshTokens.FirstOrDefault(x => x.TokenHash == tokenHash);
            if (refreshToken is null)
            {
                await WriteAuditAsync(user, user.Email, AuthAuditEventType.Logout, false, metadata, "Refresh token was not found for the user.");
                await _authRepository.SaveChangesAsync();
                throw new UnauthorizedException("Refresh token is invalid.");
            }

            RevokeRefreshToken(refreshToken, metadata, "Logged out.");
            _authRepository.UpdateUser(user);
            await WriteAuditAsync(user, user.Email, AuthAuditEventType.Logout, true, metadata, null);
            await _authRepository.SaveChangesAsync();
        }

        public async Task<UserResponseDto> GetCurrentUserAsync(int userId)
        {
            var user = await _authRepository.GetUserByIdAsync(userId)
                ?? throw new NotFoundException("User not found.");

            if (!user.IsActive)
            {
                throw new ForbiddenException("User account is deactivated.");
            }

            return user.ToResponse();
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto, AuthRequestMetadata metadata)
        {
            if (dto is null)
            {
                throw new ValidationException("Change password payload is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
            {
                throw new ValidationException("Current password is required.");
            }

            PasswordPolicyValidator.Validate(dto.NewPassword, _authOptions);

            var user = await _authRepository.GetUserByIdWithRefreshTokensAsync(userId)
                ?? throw new NotFoundException("User not found.");

            EnsureUserCanAuthenticate(user);

            if (_passwordHasher.VerifyPassword(user.PasswordHash, dto.CurrentPassword) == PasswordVerificationStatus.Failed)
            {
                await WriteAuditAsync(user, user.Email, AuthAuditEventType.ChangePassword, false, metadata, "Current password is invalid.");
                await _authRepository.SaveChangesAsync();
                throw new UnauthorizedException("Current password is invalid.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            RevokeRefreshTokens(user, metadata, "Password changed.");

            _authRepository.UpdateUser(user);
            await WriteAuditAsync(user, user.Email, AuthAuditEventType.ChangePassword, true, metadata, null);
            await _authRepository.SaveChangesAsync();
        }

        private async Task<AuthResponseDto> CreateSessionAsync(
            User user,
            AuthRequestMetadata metadata,
            AuthAuditEventType auditEventType,
            string reason)
        {
            using var activity = ApplicationTelemetry.ActivitySource.StartActivity("auth.session.create");
            activity?.SetTag("auth.event_type", auditEventType.ToString());
            activity?.SetTag("user.id", user.Id);

            var accessToken = _tokenService.CreateAccessToken(user);
            var refreshToken = _tokenService.CreateRefreshToken();

            await _authRepository.AddRefreshTokenAsync(new RefreshToken
            {
                User = user,
                TokenHash = refreshToken.TokenHash,
                ExpiresAt = refreshToken.ExpiresAtUtc,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                CreatedByIp = metadata.IpAddress,
                UserAgent = metadata.UserAgent
            });

            await WriteAuditAsync(user, user.Email, auditEventType, true, metadata, reason);
            ApplicationTelemetry.AuthEvents.Add(1, KeyValuePair.Create<string, object?>("event", auditEventType.ToString()));

            return new AuthResponseDto
            {
                AccessToken = accessToken.Token,
                AccessTokenExpiresAt = accessToken.ExpiresAtUtc,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAtUtc,
                User = user.ToResponse()
            };
        }

        private async Task<AuthResponseDto> RotateRefreshTokenAsync(RefreshToken currentToken, AuthRequestMetadata metadata)
        {
            var user = currentToken.User;
            var accessToken = _tokenService.CreateAccessToken(user);
            var newRefreshToken = _tokenService.CreateRefreshToken();

            currentToken.RevokedAt = _timeProvider.GetUtcNow().UtcDateTime;
            currentToken.RevokedByIp = metadata.IpAddress;
            currentToken.ReplacedByTokenHash = newRefreshToken.TokenHash;
            currentToken.RevocationReason = "Rotated.";

            await _authRepository.AddRefreshTokenAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newRefreshToken.TokenHash,
                SessionId = currentToken.SessionId,
                ExpiresAt = newRefreshToken.ExpiresAtUtc,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                CreatedByIp = metadata.IpAddress,
                UserAgent = metadata.UserAgent
            });

            await WriteAuditAsync(user, user.Email, AuthAuditEventType.Refresh, true, metadata, "Refresh token rotated.");
            ApplicationTelemetry.AuthEvents.Add(1, KeyValuePair.Create<string, object?>("event", AuthAuditEventType.Refresh.ToString()));

            return new AuthResponseDto
            {
                AccessToken = accessToken.Token,
                AccessTokenExpiresAt = accessToken.ExpiresAtUtc,
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiresAt = newRefreshToken.ExpiresAtUtc,
                User = user.ToResponse()
            };
        }

        private void RevokeRefreshTokens(User user, AuthRequestMetadata metadata, string reason)
        {
            foreach (var refreshToken in user.RefreshTokens.Where(x => x.RevokedAt is null && x.ExpiresAt > _timeProvider.GetUtcNow().UtcDateTime))
            {
                RevokeRefreshToken(refreshToken, metadata, reason);
            }
        }

        private void RevokeRefreshToken(RefreshToken refreshToken, AuthRequestMetadata metadata, string reason)
        {
            refreshToken.RevokedAt = _timeProvider.GetUtcNow().UtcDateTime;
            refreshToken.RevokedByIp = metadata.IpAddress;
            refreshToken.RevocationReason = reason;
        }

        private void EnsureUserCanAuthenticate(User user)
        {
            if (!user.IsActive)
            {
                throw new ForbiddenException("User account is deactivated.");
            }

            if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > _timeProvider.GetUtcNow().UtcDateTime)
            {
                throw new UnauthorizedException($"Account is locked until {user.LockoutEndUtc.Value:O}.");
            }
        }

        private async Task WriteAuditAsync(
            User? user,
            string? email,
            AuthAuditEventType eventType,
            bool isSuccessful,
            AuthRequestMetadata metadata,
            string? failureReason)
        {
            await _authRepository.AddAuditLogAsync(new AuthAuditLog
            {
                UserId = user?.Id,
                EventType = eventType,
                Email = email,
                IsSuccessful = isSuccessful,
                FailureReason = isSuccessful ? null : failureReason,
                IpAddress = metadata.IpAddress,
                UserAgent = metadata.UserAgent,
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            });
        }

        private void ValidateRegisterDto(RegisterRequestDto dto)
        {
            if (dto is null)
            {
                throw new ValidationException("Register payload is required.");
            }

            ValidateIdentityFields(dto.Email, dto.Password, dto.UserName, dto.Level);
        }

        private static void ValidateLoginDto(LoginRequestDto dto)
        {
            if (dto is null)
            {
                throw new ValidationException("Login payload is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new ValidationException("Email and password are required.");
            }
        }

        private void ValidateIdentityFields(string email, string password, string userName, LanguageLevel level)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ValidationException("Email is required.");
            }

            if (email.Trim().Length > 150 || !email.Contains('@'))
            {
                throw new ValidationException("Email format is invalid.");
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ValidationException("UserName is required.");
            }

            if (userName.Trim().Length > 100)
            {
                throw new ValidationException("UserName must not exceed 100 characters.");
            }

            PasswordPolicyValidator.Validate(password, _authOptions);

            if (!Enum.IsDefined(typeof(LanguageLevel), level))
            {
                throw new ValidationException("Invalid user level.");
            }
        }
    }
}
