# ConfigServerProxy

This is a proxy for Spring Cloud Config Server for local development purposes.

Features:

* **API compatibility** with Config Server
* **Config caching** for when getting config from the actual deployed instance is too slow
* **Automatic authentication** using OAuth2 client credentials flow
* **Utility UI** for viewing cached config and other things

## Run with docker

[config-server-proxy at DockerHub](https://hub.docker.com/r/davidkvc/config-server-proxy)

```bash
docker run -d \
	-p 8080:8080 \
	-v config-server-proxy-config.json:/app/config-server-proxy-config.json \
	davidkvc/config-server-proxy
```

## Usage

Instead of pointing your app to the Config Server, point it to this proxy with /config
subpath. Your app can then get config from:

* `/config/{application}/{profile}/{label}`
* `/config/{label}/{application}-{profile}.yml`

UI is available at http://localhost:8080/

## Configuration

Configuration is loaded from `config-server-proxy-config.json`.
See `ConfigServerProxyConfig.cs` for config structure.

TODO: properly document config

