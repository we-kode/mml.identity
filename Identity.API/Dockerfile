FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
ARG USERNAME=mml
ARG UID
ARG GID
WORKDIR /app
EXPOSE 5051
ENV ASPNETCORE_ENVIRONMENT=Release
RUN echo 'alias create="dotnet /app/cli/Identity.CLI.dll"' >> ~/.bashrc && \
    echo 'alias upload-create="dotnet /app/cli/Identity.CLI.dll -uc"' >> ~/.bashrc && \
    echo 'alias upload-list="dotnet /app/cli/Identity.CLI.dll -ul"' >> ~/.bashrc && \
    echo 'alias upload-remove="dotnet /app/cli/Identity.CLI.dll -ud"' >> ~/.bashrc

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
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

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "identity/Identity.dll"]
