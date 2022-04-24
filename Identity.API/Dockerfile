#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5051
ENV ASPNETCORE_ENVIRONMENT=Release

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG DOT_NET_BUILD_CONFIG=Release
WORKDIR /src
COPY ["Identity.API/Identity.csproj", "Identity.API/"]
COPY ["Identity.DBContext/Identity.DBContext.csproj", "Identity.DBContext/"]
COPY ["Identity.Application/Identity.Application.csproj", "Identity.Application/"]
COPY ["Identity.Infrastructure/Identity.Infrastructure.csproj", "Identity.Infrastructure/"]
RUN dotnet restore "Identity.API/Identity.csproj"
COPY . .
WORKDIR "/src/Identity.API"
RUN dotnet build "Identity.csproj" -c ${DOT_NET_BUILD_CONFIG} -o /app/build

FROM build AS publish
ARG DOT_NET_BUILD_CONFIG=Release
WORKDIR "/src/Identity.API"
RUN dotnet publish "Identity.csproj" -c ${DOT_NET_BUILD_CONFIG} -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Identity.dll"]
