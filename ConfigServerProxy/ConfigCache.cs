
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ConfigServerProxy;

public record ConfigSpec(
    string Application,
    string Profile,
    string Label,
    ConfigFormat Format);

public enum ConfigFormat
{
    FullVerbose,
    ResolvedYaml,
}

public class ConfigCache : BackgroundService
{
    private readonly HttpClient _httpClient;
    private readonly ConfigServerProxyConfig _config;
    private readonly ILogger<ConfigCache> _logger;

    private Dictionary<ConfigSpec, string> _cache = new();

    public ConfigCache(HttpClient httpClient, IOptions<ConfigServerProxyConfig> config, ILogger<ConfigCache> logger)
    {
        _config = config.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> GetConfig(ConfigSpec configSpec)
    {
        if (_cache.TryGetValue(configSpec, out var value))
        {
            return value;
        }

        return null;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var fetchTasks = from env in _config.Environments
                             from app in _config.Apps
                             select FetchConfig(env.Key, env.Value, app.Key, app.Value, stoppingToken);

            await Task.WhenAll(fetchTasks);

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task FetchConfig(string env, EnvironmentDefinition envDef, string app, AppDefinition appDef, CancellationToken cancellationToken)
    {
        var label = "main";
        var configServerUrl = envDef.ConfigServerBaseUri;
        if (!configServerUrl.EndsWith("/"))
        {
            configServerUrl += "/";
        }
        var fullVerboseUrl = $"{configServerUrl}{app}/{env}/{label}";
        var resolvedYamlUrl = $"{configServerUrl}{label}/{app}-{env}.yml";

        try
        {
            await DoFetch(fullVerboseUrl, ConfigFormat.FullVerbose);
            await DoFetch(resolvedYamlUrl, ConfigFormat.ResolvedYaml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured when fetching config from {ConfigUrl}", fullVerboseUrl);
        }

        async Task DoFetch(string url, ConfigFormat format)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);

            var client = appDef.Client;
            if (!string.IsNullOrEmpty(client))
            {
                var accessToken = await GetAccessToken(client, cancellationToken);
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }

            using var resp = await _httpClient.SendAsync(req, cancellationToken);
            resp.EnsureSuccessStatusCode();

            var spec = new ConfigSpec(app, env, label, format);
            var configData = await resp.Content.ReadAsStringAsync();
            lock (_cache)
            {
                _cache[spec] = configData;
            }
        }
    }

    private async Task<string> GetAccessToken(string client, CancellationToken cancellationToken)
    {
        var clientDef = _config.Clients.GetValueOrDefault(client);
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

