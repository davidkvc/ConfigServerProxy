using ConfigServerProxy;
using ConfigServerProxy.Components;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("config-server-proxy-config.json", true, true);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ConfigCache>();
builder.Services.AddSingleton<ClientAuthentication>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ConfigCache>());

builder.Services.AddOptions<ConfigServerProxyConfig>()
    .Bind(builder.Configuration);

var app = builder.Build();

app.MapOpenApi();

app.MapGet("/config/{application}/{profile}/{label}", async (string application, string profile, string label,
    ConfigCache configCache) =>
{
    var config = await configCache.GetConfig(new(application, profile, label, ConfigFormat.FullVerbose));
    if (config is null)
    {
        return Results.BadRequest(new
        {
            Error = "Config not available",
        });
    }

    return Results.Content(config, "application/json", Encoding.UTF8);
});

app.MapGet("/config/{label}/{application}-{profile}.yml", async (string application, string profile, string label,
    ConfigCache configCache) =>
{
    var config = await configCache.GetConfig(new(application, profile, label, ConfigFormat.ResolvedYaml));
    if (config is null)
    {
        return Results.BadRequest(new
        {
            Error = "Config not available",
        });
    }

    return Results.Content(config, "application/yaml", Encoding.UTF8);
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();