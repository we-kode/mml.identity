FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
#ARG USERNAME=mml
ARG UID
ARG GID
#RUN groupadd --gid $GID $USERNAME \
#    && useradd --uid $UID --gid $GID -m $USERNAME
#USER $USERNAME
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 5051
ENV ASPNETCORE_ENVIRONMENT=Release
RUN echo 'alias create="dotnet /app/cli/Identity.CLI.dll"' >> ~/.bashrc && \
    echo 'alias admin-create="dotnet /app/cli/Identity.CLI.dll -ac"' >> ~/.bashrc && \
    echo 'alias admin-list="dotnet /app/cli/Identity.CLI.dll -al"' >> ~/.bashrc && \
    echo 'alias admin-remove="dotnet /app/cli/Identity.CLI.dll -ad"' >> ~/.bashrc

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG DOT_NET_BUILD_CONFIG=Release
WORKDIR /src
COPY ["Identity.API/Identity.csproj", "Identity.API/"]
COPY ["Identity.DBContext/Identity.DBContext.csproj", "Identity.DBContext/"]
COPY ["Identity.Application/Identity.Application.csproj", "Identity.Application/"]
COPY ["Identity.Infrastructure/Identity.Infrastructure.csproj", "Identity.Infrastructure/"]
COPY ["Identity.CLI/Identity.CLI.csproj", "Identity.CLI/"]
RUN dotnet restore "Identity.API/Identity.csproj"
RUN dotnet restore "Identity.CLI/Identity.CLI.csproj"
COPY . .
WORKDIR "/src/Identity.API"
RUN dotnet build "Identity.csproj" -c ${DOT_NET_BUILD_CONFIG} -o /app/build/identity

WORKDIR "/src/Identity.CLI"
RUN dotnet build "Identity.CLI.csproj" -c ${DOT_NET_BUILD_CONFIG} -o /app/build/cli

FROM build AS publish
ARG DOT_NET_BUILD_CONFIG=Release
WORKDIR "/src/Identity.API"
RUN dotnet publish "Identity.csproj" -c ${DOT_NET_BUILD_CONFIG} -o /app/publish/identity

WORKDIR "/src/Identity.CLI"
RUN dotnet publish "Identity.CLI.csproj" -c ${DOT_NET_BUILD_CONFIG} -o /app/publish/cli

# Third-party notices of all NuGet packages of the service and the CLI.
FROM build AS licenses
WORKDIR /src
RUN dotnet tool install nuget-license --version 4.0.18 --tool-path /tools
RUN echo '["Identity.API/Identity.csproj","Identity.CLI/Identity.CLI.csproj"]' > /tmp/projects.json &&     /tools/nuget-license -ji /tmp/projects.json -t -ignore WeKoDe.MML.Messages -o Markdown     -fo /licenses/THIRD-PARTY-NOTICES.md -d /licenses/texts
# Content embedded in NuGet packages under a license other than the package license.
RUN printf '\n## Embedded content\n\n| Content | Contained in | Copyright | License |\n| --- | --- | --- | --- |\n| [EFF Large Wordlist](https://www.eff.org/dice) | PasswordGenerator | Electronic Frontier Foundation | [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) |\n' >> /licenses/THIRD-PARTY-NOTICES.md

FROM base AS final
WORKDIR /app
RUN mkdir -p /secrets
COPY --from=publish /app/publish .
COPY --from=licenses /licenses ./licenses
ENTRYPOINT ["dotnet", "identity/Identity.dll"]
