using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ConfigServerProxy;

public class ClientAuthentication
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ConfigServerProxyConfig> _config;

    public ClientAuthentication(HttpClient httpClient, IOptionsMonitor<ConfigServerProxyConfig> config)
    {
        _httpClient = httpClient;
        _config = config;
    }


    public async Task<string> GetAccessToken(string client, CancellationToken cancellationToken)
    {
        var clientDef = _config.CurrentValue.Clients.GetValueOrDefault(client);
        if (clientDef == null)
        {
            throw new Exception($"No definition found for client {client}");
        }

        var content = new FormUrlEncodedContent([
            new ("grant_type", "client_credentials"),
            new ("client_id", clientDef.ClientId),
            new ("client_secret", clientDef.ClientSecret),
        ]);

        using var req = new HttpRequestMessage(HttpMethod.Post, clientDef.TokenUri);
        req.Content = content;

        using var resp = await _httpClient.SendAsync(req, cancellationToken);
        resp.EnsureSuccessStatusCode();

        var tokenData = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return tokenData.GetProperty("access_token").GetString();
    }
}
