using Flights.Api.Authentication;
using Flights.Api.Mcp;
using Flights.Api.Models;
using Flights.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddSingleton<FlightService>();
builder.Services.AddSingleton<ClientRegistrationStore>();

// Add CORS policy to allow any web application to call the API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Authentication with multiple schemes
builder.Services.AddAuthentication(options =>
{
    // Default to our simple OAuth scheme
    options.DefaultAuthenticateScheme = SimpleOAuthAuthenticationHandler.SchemeName;
    options.DefaultChallengeScheme = SimpleOAuthAuthenticationHandler.SchemeName;
})
.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, SimpleOAuthAuthenticationHandler>(
    SimpleOAuthAuthenticationHandler.SchemeName, options => { })
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    // Also support Azure AD JWT tokens
    options.Authority = $"{builder.Configuration["AzureAd:Instance"]}{builder.Configuration["AzureAd:TenantId"]}";
    options.Audience = builder.Configuration["AzureAd:Audience"];
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization();

// Add MCP server with tools
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<FlightTools>();

builder.Services.AddOpenApi();

// Configure JSON options to ignore null values (required for OAuth responses)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Helper method to get the actual public base URL (handles dev tunnels and proxies)
static string GetBaseUrl(HttpContext context)
{
    // Check ALL possible forwarded host headers (different proxies use different headers)
    var forwardedHost = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault()
                     ?? context.Request.Headers["X-Original-Host"].FirstOrDefault()  
                     ?? context.Request.Headers["Host"].FirstOrDefault();
    
    // Check for forwarded protocol
    var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault()
                      ?? context.Request.Headers["X-Forwarded-Scheme"].FirstOrDefault();
    
    // For dev tunnels specifically, check if Host header contains devtunnels.ms
    var hostHeader = context.Request.Host.ToString();
    if (hostHeader.Contains("devtunnels.ms", StringComparison.OrdinalIgnoreCase))
    {
        // Direct dev tunnel request - use the Host header
        return $"https://{hostHeader}{context.Request.PathBase}";
    }
    
    // Check if we have forwarded headers
    if (!string.IsNullOrEmpty(forwardedHost) && forwardedHost != "localhost:7277")
    {
        var scheme = !string.IsNullOrEmpty(forwardedProto) ? forwardedProto : "https";
        // Remove port if it's standard (80/443)
        if (forwardedHost.EndsWith(":443") || forwardedHost.EndsWith(":80"))
        {
            forwardedHost = forwardedHost.Substring(0, forwardedHost.LastIndexOf(':'));
        }
        return $"{scheme}://{forwardedHost}{context.Request.PathBase}";
    }
    
    // Fallback to regular request URL
    return $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}";
}

var app = builder.Build();

// Configure forwarded headers for dev tunnels and reverse proxies
// IMPORTANT: Must be before UseRouting() and other middleware
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost | 
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor,
    // Required for dev tunnels to work
    RequireHeaderSymmetry = false,
    ForwardLimit = null
};

// Trust all proxies (dev tunnels, nginx, etc.)
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeadersOptions);

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

// Enable CORS
app.UseCors();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();


// OpenID Connect Discovery endpoint (RFC 8414 / OpenID Connect Discovery 1.0)
app.MapGet("/.well-known/openid-configuration", (HttpContext context) =>
{
    var baseUrl = GetBaseUrl(context);
    
    var discoveryDocument = new OpenIdConnectDiscoveryDocument
    {
        // Our API acts as the issuer for MCP/Copilot Studio integration
        Issuer = baseUrl,
        // Use our own authorization endpoint
        AuthorizationEndpoint = $"{baseUrl}/oauth/authorize",
        // Use our own token endpoint
        TokenEndpoint = $"{baseUrl}/oauth/token",
        // Point to our JWKS endpoint
        JwksUri = $"{baseUrl}/.well-known/jwks.json",
        RegistrationEndpoint = $"{baseUrl}/oauth/register",
        ScopesSupported = ["openid", "profile", "email", "Flights.Read"],
        ResponseTypesSupported = ["code", "token", "id_token", "code id_token", "code token", "id_token token", "code id_token token"],
        GrantTypesSupported = ["authorization_code", "client_credentials", "refresh_token", "implicit"],
        SubjectTypesSupported = ["public"],
        IdTokenSigningAlgValuesSupported = ["RS256"],
        TokenEndpointAuthMethodsSupported = ["client_secret_post", "client_secret_basic", "private_key_jwt", "none"],
        IntrospectionEndpoint = $"{baseUrl}/oauth/introspect",
        RevocationEndpoint = $"{baseUrl}/oauth/revoke"
    };
    
    return Results.Ok(discoveryDocument);
})
.WithName("OpenIdConfiguration")
.WithDescription("OpenID Connect Discovery endpoint")
.AllowAnonymous();

// OAuth 2.0 Dynamic Client Registration endpoint (RFC 7591)
app.MapPost("/oauth/register", (
    ClientRegistrationRequest request,
    ClientRegistrationStore store,
    HttpContext context) =>
{
    // Generate client credentials
    var clientId = Guid.NewGuid().ToString("N");
    var clientSecret = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + 
                       Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    
    var issuedAt = DateTime.UtcNow;
    var expiresAt = issuedAt.AddYears(1); // Client credentials expire in 1 year
    
    // Create registered client
    var registeredClient = new RegisteredClient
    {
        ClientId = clientId,
        ClientSecret = clientSecret,
        IssuedAt = issuedAt,
        ExpiresAt = expiresAt,
        RedirectUris = request.RedirectUris,
        TokenEndpointAuthMethod = request.TokenEndpointAuthMethod ?? "client_secret_post",
        GrantTypes = request.GrantTypes ?? ["authorization_code", "client_credentials"],
        ResponseTypes = request.ResponseTypes ?? ["code"],
        ClientName = request.ClientName,
        Scope = request.Scope ?? "Flights.Read"
    };
    
    // Store the client
    store.RegisterClient(registeredClient);
    
    var baseUrl = GetBaseUrl(context);
    
    // Return registration response (only include non-null values)
    var response = new ClientRegistrationResponse
    {
        ClientId = clientId,
        ClientSecret = clientSecret,
        ClientIdIssuedAt = new DateTimeOffset(issuedAt).ToUnixTimeSeconds(),
        ClientSecretExpiresAt = new DateTimeOffset(expiresAt).ToUnixTimeSeconds(),
        RedirectUris = registeredClient.RedirectUris,
        TokenEndpointAuthMethod = registeredClient.TokenEndpointAuthMethod,
        GrantTypes = registeredClient.GrantTypes,
        ResponseTypes = registeredClient.ResponseTypes,
        ClientName = registeredClient.ClientName,
        ClientUri = request.ClientUri, // Only include if provided
        LogoUri = request.LogoUri, // Only include if provided
        Scope = registeredClient.Scope,
        Contacts = request.Contacts, // Only include if provided
        RegistrationClientUri = $"{baseUrl}/oauth/register/{clientId}"
        // RegistrationAccessToken is null - will be omitted from JSON
    };
    
    return Results.Ok(response);
})
.WithName("RegisterClient")
.WithDescription("OAuth 2.0 Dynamic Client Registration endpoint")
.AllowAnonymous();

// OAuth 2.0 Authorization endpoint (for authorization code flow)
app.MapGet("/oauth/authorize", (
    HttpContext context,
    ClientRegistrationStore store,
    string client_id,
    string? redirect_uri,
    string? response_type,
    string? scope,
    string? state) =>
{
    // Validate client exists
    var client = store.GetClient(client_id);
    if (client == null)
    {
        return Results.BadRequest(new { error = "invalid_client", error_description = "Client not found" });
    }
    
    // Validate redirect URI
    if (!string.IsNullOrEmpty(redirect_uri) && client.RedirectUris != null && 
        !client.RedirectUris.Contains(redirect_uri))
    {
        return Results.BadRequest(new { error = "invalid_request", error_description = "Invalid redirect_uri" });
    }
    
    // For simplicity, auto-approve and redirect back with authorization code
    // In production, you would show a consent page here
    var authCode = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    
    // Store authorization code temporarily (in production, use a proper store with expiration)
    var authCodeKey = $"authcode_{authCode}";
    context.RequestServices.GetRequiredService<ClientRegistrationStore>();
    
    // Build redirect URL with authorization code
    var redirectUrl = redirect_uri ?? client.RedirectUris?.FirstOrDefault() ?? "about:blank";
    var separator = redirectUrl.Contains('?') ? '&' : '?';
    
    return Results.Redirect($"{redirectUrl}{separator}code={authCode}&state={state}");
})
.WithName("OAuthAuthorize")
.WithDescription("OAuth 2.0 Authorization endpoint")
.AllowAnonymous();

// OAuth 2.0 Token endpoint (exchange code for token or client credentials)
app.MapPost("/oauth/token", async (
    HttpContext context,
    ClientRegistrationStore store) =>
{
    string? clientId = null;
    string? clientSecret = null;
    string? grantType = null;
    
    // Try to get client credentials from multiple sources per OAuth 2.0 spec
    
    // 1. Check Authorization header (HTTP Basic Authentication)
    var authHeader = context.Request.Headers.Authorization.ToString();
    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var base64Credentials = authHeader.Substring("Basic ".Length).Trim();
            var credentials = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Credentials));
            var parts = credentials.Split(':', 2);
            if (parts.Length == 2)
            {
                clientId = parts[0];
                clientSecret = parts[1];
            }
        }
        catch
        {
            // Invalid Basic Auth header, will try other methods
        }
    }
    
    // 2. Check form body (client_secret_post method)
    var form = await context.Request.ReadFormAsync();
    grantType = form["grant_type"].ToString();
    
    if (string.IsNullOrEmpty(clientId))
    {
        clientId = form["client_id"].ToString();
    }
    if (string.IsNullOrEmpty(clientSecret))
    {
        clientSecret = form["client_secret"].ToString();
    }
    
    // 3. Check JSON body (some clients send JSON instead of form data)
    if (string.IsNullOrEmpty(clientId) && context.Request.ContentType?.Contains("application/json") == true)
    {
        try
        {
            context.Request.Body.Position = 0;
            var jsonBody = await System.Text.Json.JsonDocument.ParseAsync(context.Request.Body);
            if (jsonBody.RootElement.TryGetProperty("client_id", out var cidElement))
                clientId = cidElement.GetString();
            if (jsonBody.RootElement.TryGetProperty("client_secret", out var csElement))
                clientSecret = csElement.GetString();
            if (string.IsNullOrEmpty(grantType) && jsonBody.RootElement.TryGetProperty("grant_type", out var gtElement))
                grantType = gtElement.GetString();
        }
        catch
        {
            // JSON parsing failed, continue with form data
        }
    }
    
    // Validate we have credentials
    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    {
        context.Response.StatusCode = 400; // Bad Request
        return Results.Json(new
        {
            error = "invalid_request",
            error_description = "Missing client_id or client_secret. Provide via Basic Auth header or request body."
        }, statusCode: 400);
    }
    
    // Validate client credentials
    if (!store.ValidateClient(clientId, clientSecret))
    {
        context.Response.StatusCode = 401; // Unauthorized
        context.Response.Headers.Append("WWW-Authenticate", "Bearer");
        return Results.Json(new
        {
            error = "invalid_client",
            error_description = "Client authentication failed"
        }, statusCode: 401);
    }
    
    var client = store.GetClient(clientId);
    
    // Validate grant type
    if (string.IsNullOrEmpty(grantType))
    {
        context.Response.StatusCode = 400;
        return Results.Json(new
        {
            error = "invalid_request",
            error_description = "Missing grant_type parameter"
        }, statusCode: 400);
    }
    
    // Generate access token (in production, use proper JWT)
    var accessToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + 
                     Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    
    var tokenResponse = new
    {
        access_token = accessToken,
        token_type = "Bearer",
        expires_in = 3600,
        scope = client?.Scope ?? "Flights.Read",
        // For implicit flow, also return id_token
        id_token = grantType == "implicit" ? GenerateIdToken(clientId) : null
    };
    
    return Results.Ok(tokenResponse);
})
.WithName("OAuthToken")
.WithDescription("OAuth 2.0 Token endpoint")
.AllowAnonymous();

// OAuth 2.0 Token Introspection endpoint (RFC 7662)
app.MapPost("/oauth/introspect", async (
    HttpContext context,
    ClientRegistrationStore store) =>
{
    var form = await context.Request.ReadFormAsync();
    var token = form["token"].ToString();
    var clientId = form["client_id"].ToString();
    var clientSecret = form["client_secret"].ToString();
    
    // Validate client
    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    {
        context.Response.StatusCode = 401;
        context.Response.Headers.Append("WWW-Authenticate", "Bearer");
        return Results.Json(new
        {
            error = "invalid_client",
            error_description = "Client authentication required"
        }, statusCode: 401);
    }
    
    if (!store.ValidateClient(clientId, clientSecret))
    {
        context.Response.StatusCode = 401;
        context.Response.Headers.Append("WWW-Authenticate", "Bearer");
        return Results.Json(new
        {
            error = "invalid_client",
            error_description = "Client authentication failed"
        }, statusCode: 401);
    }
    
    // For simplicity, accept all non-empty tokens as active
    // In production, validate token signature, expiration, etc.
    var introspectionResponse = new
    {
        active = !string.IsNullOrEmpty(token),
        scope = "Flights.Read",
        client_id = clientId,
        token_type = "Bearer",
        exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
        iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
    };
    
    return Results.Ok(introspectionResponse);
})
.WithName("OAuthIntrospect")
.WithDescription("OAuth 2.0 Token Introspection endpoint (RFC 7662)")
.AllowAnonymous();

// OAuth 2.0 Token Revocation endpoint (RFC 7009)
app.MapPost("/oauth/revoke", async (
    HttpContext context,
    ClientRegistrationStore store) =>
{
    var form = await context.Request.ReadFormAsync();
    var token = form["token"].ToString();
    var clientId = form["client_id"].ToString();
    var clientSecret = form["client_secret"].ToString();
    
    // Validate client
    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    {
        context.Response.StatusCode = 401;
        context.Response.Headers.Append("WWW-Authenticate", "Bearer");
        return Results.Json(new
        {
            error = "invalid_client",
            error_description = "Client authentication required"
        }, statusCode: 401);
    }
    
    if (!store.ValidateClient(clientId, clientSecret))
    {
        context.Response.StatusCode = 401;
        context.Response.Headers.Append("WWW-Authenticate", "Bearer");
        return Results.Json(new
        {
            error = "invalid_client",
            error_description = "Client authentication failed"
        }, statusCode: 401);
    }
    
    // In production, revoke the token from the token store
    // For now, just return success (200 OK with empty body per RFC 7009)
    context.Response.StatusCode = 200;
    return Results.Ok();
})
.WithName("OAuthRevoke")
.WithDescription("OAuth 2.0 Token Revocation endpoint (RFC 7009)")
.AllowAnonymous();

// Simple ID token generator (for demonstration only)
static string GenerateIdToken(string clientId)
{
    // In production, generate a proper JWT
    var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{{\"sub\":\"{clientId}\",\"aud\":\"{clientId}\"}}"));
    return $"eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.{payload}.signature";
}

// JWKS endpoint (JSON Web Key Set) - required for token verification
app.MapGet("/.well-known/jwks.json", () =>
{
    // Return empty JWKS for now (in production, include actual signing keys)
    return Results.Ok(new { keys = Array.Empty<object>() });
})
.WithName("JWKS")
.WithDescription("JSON Web Key Set endpoint")
.AllowAnonymous();

// OAuth 2.0 Protected Resource Metadata endpoint (RFC 8414 / MCP 2025-06-18 spec)
app.MapGet("/.well-known/oauth-protected-resource", (HttpContext context) =>
{
    var baseUrl = GetBaseUrl(context);
    
    var resourceMetadata = new
    {
        // Resource server identifier
        resource = baseUrl,
        // Authorization servers that protect this resource
        authorization_servers = new[] { baseUrl },
        // Supported bearer token methods
        bearer_methods_supported = new[] { "header", "body", "query" },
        // Resource server capabilities
        resource_documentation = $"{baseUrl}/docs",
        // Supported scopes
        scopes_supported = new[] { "Flights.Read", "openid", "profile", "email" },
        // Token introspection endpoint (if available)
        introspection_endpoint = $"{baseUrl}/oauth/introspect",
        // Revocation endpoint (if available)
        revocation_endpoint = $"{baseUrl}/oauth/revoke"
    };
    
    return Results.Ok(resourceMetadata);
})
.WithName("OAuthProtectedResourceMetadata")
.WithDescription("OAuth 2.0 Protected Resource Metadata endpoint (RFC 8414)")
.AllowAnonymous();

// Get registered client information (optional, for management)
app.MapGet("/oauth/register/{clientId}", (
    string clientId,
    ClientRegistrationStore store) =>
{
    var client = store.GetClient(clientId);
    if (client == null)
    {
        return Results.NotFound(new { error = "invalid_client_id", error_description = "Client not found" });
    }
    
    return Results.Ok(new
    {
        client_id = client.ClientId,
        client_id_issued_at = new DateTimeOffset(client.IssuedAt).ToUnixTimeSeconds(),
        client_secret_expires_at = client.ExpiresAt.HasValue ? 
            (long?)new DateTimeOffset(client.ExpiresAt.Value).ToUnixTimeSeconds() : null,
        redirect_uris = client.RedirectUris,
        grant_types = client.GrantTypes,
        response_types = client.ResponseTypes,
        client_name = client.ClientName,
        scope = client.Scope
    });
})
.WithName("GetRegisteredClient")
.WithDescription("Get information about a registered client")
.AllowAnonymous();

// Map MCP server endpoint (protected)
app.MapMcp("/mcp").RequireAuthorization();

// Search flights endpoint
app.MapGet("/flights/search", (
    FlightService flightService,
    string? origin,
    string? destination,
    DateOnly? departureDate,
    int maxResults = 10) =>
{
    var request = new FlightSearchRequest(origin, destination, departureDate, maxResults);
    var flights = flightService.SearchFlights(request);
    return Results.Ok(flights);
})
.WithName("SearchFlights")
.WithDescription("Search for flights by origin, destination, and departure date")
.RequireAuthorization();

// Get flight by flight number
app.MapGet("/flights/{flightNumber}", (FlightService flightService, string flightNumber) =>
{
    var flight = flightService.GetFlightByNumber(flightNumber);
    return flight is not null ? Results.Ok(flight) : Results.NotFound();
})
.WithName("GetFlightByNumber")
.WithDescription("Get detailed information about a specific flight by flight number")
.RequireAuthorization();

// Get all available origins
app.MapGet("/flights/airports/origins", (FlightService flightService) =>
{
    var origins = flightService.GetAvailableOrigins();
    return Results.Ok(origins);
})
.WithName("GetAvailableOrigins")
.WithDescription("Get list of all available departure airports")
.RequireAuthorization();

// Get all available destinations
app.MapGet("/flights/airports/destinations", (FlightService flightService) =>
{
    var destinations = flightService.GetAvailableDestinations();
    return Results.Ok(destinations);
})
.WithName("GetAvailableDestinations")
.WithDescription("Get list of all available destination airports")
.RequireAuthorization();

app.Run();
