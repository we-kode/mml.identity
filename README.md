# IdentityService

## Build docker image local

To build the docker file run:

```bash
docker build -f .\Identity.API\Dockerfile [--build-arg DOT_NET_BUILD_CONFIG=Release|Debug] -t git.knet/wekode/gospel-academy/identityservice:dev .
```

If the Dockerfile is build in `Release` mode defined integration test will be executed on build time. In Debug mode tests will be skipped. Default build mode is Release.

## Debug

To debug the running service in kubernetes a remote debugger will be running in docker when the docker was build with environment Variable `ASPNETCORE_ENVIRONMENT=Development`. 
You can attach to this debugger.

### Attach with Visual Studio

Use Kubernetes bridge. See https://docs.microsoft.com/en-us/visualstudio/bridge/bridge-to-kubernetes-vs

### Attach with Visual Studio Code

Use Kubernetes bridge. See https://docs.microsoft.com/en-us/visualstudio/bridge/bridge-to-kubernetes-vs-code