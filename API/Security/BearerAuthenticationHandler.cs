using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.Common.Security;
using Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace API.Security
{
    public class BearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ITokenService _tokenService;
        private readonly IAuthRepository _authRepository;

        public BearerAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ITokenService tokenService,
            IAuthRepository authRepository)
            : base(options, logger, encoder)
        {
            _tokenService = tokenService;
            _authRepository = authRepository;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authorizationHeader = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authorizationHeader)
                || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.NoResult();
            }

            var token = authorizationHeader["Bearer ".Length..].Trim();
            var validationResult = _tokenService.ValidateAccessToken(token);
            if (!validationResult.Succeeded || validationResult.UserId is null)
            {
                return AuthenticateResult.Fail(validationResult.FailureReason ?? "Access token is invalid.");
            }

            var user = await _authRepository.GetUserByIdAsync(validationResult.UserId.Value);
            if (user is null || !user.IsActive)
            {
                return AuthenticateResult.Fail("User account is unavailable.");
            }

            if (!string.Equals(user.SecurityStamp, validationResult.SecurityStamp, StringComparison.Ordinal))
            {
                return AuthenticateResult.Fail("Token is no longer valid.");
            }

            var identity = new ClaimsIdentity(validationResult.Claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
    }
}
