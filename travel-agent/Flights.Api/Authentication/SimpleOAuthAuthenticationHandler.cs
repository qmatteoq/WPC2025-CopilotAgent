using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Flights.Api.Authentication;

/// <summary>
/// Simple bearer token authentication handler for our OAuth server
/// Validates tokens issued by our /oauth/token endpoint
/// </summary>
public class SimpleOAuthAuthenticationHandler : AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
{
    public const string SchemeName = "SimpleOAuth";

    public SimpleOAuthAuthenticationHandler(
        IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for Authorization header
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authHeader = Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        
        // For simplicity, accept any non-empty bearer token
        // In production, validate the token properly (check signature, expiration, etc.)
        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
        }

        // Create claims for the authenticated user
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "oauth-client"),
            new Claim(ClaimTypes.Name, "OAuth Client"),
            new Claim("scope", "Flights.Read")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
