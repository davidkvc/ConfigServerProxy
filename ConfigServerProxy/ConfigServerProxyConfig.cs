namespace ConfigServerProxy;

public class ConfigServerProxyConfig
{
    public Dictionary<string, EnvironmentDefinition> Environments { get; set; } = new();
    public Dictionary<string, AppDefinition> Apps { get; set; } = new();
    public Dictionary<string, ClientDefinition> Clients { get; set; } = new();
}

public class EnvironmentDefinition
{
    public string ConfigServerBaseUri { get; set; }
}

public class AppDefinition
{
    public string Client { get; set; }
}

public class ClientDefinition
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string TokenUri { get; set; }
    public string Scope { get; set; }
}
