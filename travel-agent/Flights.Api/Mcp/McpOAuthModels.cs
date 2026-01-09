namespace Flights.Api.Mcp;

/// <summary>
/// OAuth configuration for MCP clients
/// </summary>
public record McpOAuthConfig
{
    public required string AuthorizationEndpoint { get; init; }
    public required string TokenEndpoint { get; init; }
    public required string[] GrantTypes { get; init; }
}

/// <summary>
/// OAuth token response
/// </summary>
public record TokenResponse
{
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public required int ExpiresIn { get; init; }
    public string? Scope { get; init; }
    public string? RefreshToken { get; init; }
}

/// <summary>
/// State management for OAuth flow
/// </summary>
public class OAuthStateManager
{
    private readonly Dictionary<string, OAuthState> _states = new();
    private readonly object _lock = new();

    public string CreateState(string redirectUri, string? codeChallenge = null)
    {
        var state = Guid.NewGuid().ToString();
        lock (_lock)
        {
            _states[state] = new OAuthState
            {
                RedirectUri = redirectUri,
                CodeChallenge = codeChallenge,
                CreatedAt = DateTime.UtcNow
            };
        }
        return state;
    }

    public OAuthState? GetAndRemoveState(string state)
    {
        lock (_lock)
        {
            if (_states.TryGetValue(state, out var oauthState))
            {
                _states.Remove(state);
                // Only valid for 10 minutes
                if (DateTime.UtcNow - oauthState.CreatedAt < TimeSpan.FromMinutes(10))
                {
                    return oauthState;
                }
            }
        }
        return null;
    }

    public void Cleanup()
    {
        lock (_lock)
        {
            var expiredKeys = _states
                .Where(kvp => DateTime.UtcNow - kvp.Value.CreatedAt > TimeSpan.FromMinutes(10))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _states.Remove(key);
            }
        }
    }
}

public record OAuthState
{
    public required string RedirectUri { get; init; }
    public string? CodeChallenge { get; init; }
    public DateTime CreatedAt { get; init; }
}
