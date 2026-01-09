using Flights.Api.Models;
using System.Collections.Concurrent;

namespace Flights.Api.Services;

/// <summary>
/// In-memory store for dynamically registered OAuth clients
/// In production, this should be replaced with a persistent store (database, Redis, etc.)
/// </summary>
public class ClientRegistrationStore
{
    private readonly ConcurrentDictionary<string, RegisteredClient> _clients = new();
    private readonly ILogger<ClientRegistrationStore> _logger;

    public ClientRegistrationStore(ILogger<ClientRegistrationStore> logger)
    {
        _logger = logger;
    }

    public void RegisterClient(RegisteredClient client)
    {
        _clients[client.ClientId] = client;
        _logger.LogInformation("Registered new client: {ClientId} - {ClientName}", 
            client.ClientId, client.ClientName ?? "Unnamed");
    }

    public RegisteredClient? GetClient(string clientId)
    {
        _clients.TryGetValue(clientId, out var client);
        return client;
    }

    public bool ValidateClient(string clientId, string clientSecret)
    {
        var client = GetClient(clientId);
        if (client == null)
        {
            _logger.LogWarning("Client validation failed: Client not found - {ClientId}", clientId);
            return false;
        }

        if (client.ExpiresAt.HasValue && client.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Client validation failed: Client expired - {ClientId}", clientId);
            return false;
        }

        var isValid = client.ClientSecret == clientSecret;
        if (!isValid)
        {
            _logger.LogWarning("Client validation failed: Invalid secret - {ClientId}", clientId);
        }

        return isValid;
    }

    public IEnumerable<RegisteredClient> GetAllClients()
    {
        return _clients.Values;
    }

    public bool RemoveClient(string clientId)
    {
        var removed = _clients.TryRemove(clientId, out _);
        if (removed)
        {
            _logger.LogInformation("Removed client: {ClientId}", clientId);
        }
        return removed;
    }
}
