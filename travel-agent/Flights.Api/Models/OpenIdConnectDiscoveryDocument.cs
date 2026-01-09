using System.Text.Json.Serialization;

namespace Flights.Api.Models;

/// <summary>
/// OpenID Connect Discovery Document as per:
/// https://openid.net/specs/openid-connect-discovery-1_0.html
/// </summary>
public class OpenIdConnectDiscoveryDocument
{
    [JsonPropertyName("issuer")]
    public required string Issuer { get; set; }

    [JsonPropertyName("authorization_endpoint")]
    public required string AuthorizationEndpoint { get; set; }

    [JsonPropertyName("token_endpoint")]
    public required string TokenEndpoint { get; set; }

    [JsonPropertyName("jwks_uri")]
    public required string JwksUri { get; set; }

    [JsonPropertyName("registration_endpoint")]
    public required string RegistrationEndpoint { get; set; }

    [JsonPropertyName("scopes_supported")]
    public required string[] ScopesSupported { get; set; }

    [JsonPropertyName("response_types_supported")]
    public required string[] ResponseTypesSupported { get; set; }

    [JsonPropertyName("grant_types_supported")]
    public required string[] GrantTypesSupported { get; set; }

    [JsonPropertyName("subject_types_supported")]
    public required string[] SubjectTypesSupported { get; set; }

    [JsonPropertyName("id_token_signing_alg_values_supported")]
    public required string[] IdTokenSigningAlgValuesSupported { get; set; }

    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public required string[] TokenEndpointAuthMethodsSupported { get; set; }

    [JsonPropertyName("introspection_endpoint")]
    public string? IntrospectionEndpoint { get; set; }

    [JsonPropertyName("revocation_endpoint")]
    public string? RevocationEndpoint { get; set; }
}
