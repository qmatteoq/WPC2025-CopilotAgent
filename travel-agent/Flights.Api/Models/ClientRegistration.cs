using System.Text.Json.Serialization;

namespace Flights.Api.Models;

/// <summary>
/// OAuth 2.0 Dynamic Client Registration Request as per RFC 7591
/// https://datatracker.ietf.org/doc/html/rfc7591
/// </summary>
public class ClientRegistrationRequest
{
    [JsonPropertyName("redirect_uris")]
    public string[]? RedirectUris { get; set; }

    [JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; set; }

    [JsonPropertyName("grant_types")]
    public string[]? GrantTypes { get; set; }

    [JsonPropertyName("response_types")]
    public string[]? ResponseTypes { get; set; }

    [JsonPropertyName("client_name")]
    public string? ClientName { get; set; }

    [JsonPropertyName("client_uri")]
    public string? ClientUri { get; set; }

    [JsonPropertyName("logo_uri")]
    public string? LogoUri { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("contacts")]
    public string[]? Contacts { get; set; }

    [JsonPropertyName("tos_uri")]
    public string? TosUri { get; set; }

    [JsonPropertyName("policy_uri")]
    public string? PolicyUri { get; set; }

    [JsonPropertyName("jwks_uri")]
    public string? JwksUri { get; set; }

    [JsonPropertyName("software_id")]
    public string? SoftwareId { get; set; }

    [JsonPropertyName("software_version")]
    public string? SoftwareVersion { get; set; }
}

/// <summary>
/// OAuth 2.0 Dynamic Client Registration Response as per RFC 7591
/// </summary>
public class ClientRegistrationResponse
{
    [JsonPropertyName("client_id")]
    public required string ClientId { get; set; }

    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("client_id_issued_at")]
    public long? ClientIdIssuedAt { get; set; }

    [JsonPropertyName("client_secret_expires_at")]
    public long? ClientSecretExpiresAt { get; set; }

    [JsonPropertyName("redirect_uris")]
    public string[]? RedirectUris { get; set; }

    [JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; set; }

    [JsonPropertyName("grant_types")]
    public string[]? GrantTypes { get; set; }

    [JsonPropertyName("response_types")]
    public string[]? ResponseTypes { get; set; }

    [JsonPropertyName("client_name")]
    public string? ClientName { get; set; }

    [JsonPropertyName("client_uri")]
    public string? ClientUri { get; set; }

    [JsonPropertyName("logo_uri")]
    public string? LogoUri { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("contacts")]
    public string[]? Contacts { get; set; }

    [JsonPropertyName("registration_client_uri")]
    public string? RegistrationClientUri { get; set; }

    [JsonPropertyName("registration_access_token")]
    public string? RegistrationAccessToken { get; set; }
}

/// <summary>
/// Registered client information for storage
/// </summary>
public class RegisteredClient
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public required DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string[]? RedirectUris { get; set; }
    public string? TokenEndpointAuthMethod { get; set; }
    public string[]? GrantTypes { get; set; }
    public string[]? ResponseTypes { get; set; }
    public string? ClientName { get; set; }
    public string? Scope { get; set; }
}
