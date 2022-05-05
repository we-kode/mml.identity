#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5051
ENV ASPNETCORE_ENVIRONMENT=Release
RUN echo 'alias create="dotnet /app/create/Identity.Create.dll"' >> ~/.bashrc
RUN echo 'alias upload-create="dotnet /app/create/Identity.Create.dll -uc"' >> ~/.bashrc
RUN echo 'alias upload-list="dotnet /app/create/Identity.Create.dll -ul"' >> ~/.bashrc
RUN echo 'alias upload-remove="dotnet /app/create/Identity.Create.dll -ud"' >> ~/.bashrc

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG DOT_NET_BUILD_CONFIG=Release
WORKDIR /src
COPY ["Identity.API/Identity.csproj", "Identity.API/"]
COPY ["Identity.DBContext/Identity.DBContext.csproj", "Identity.DBContext/"]
COPY ["Identity.Application/Identity.Application.csproj", "Identity.Application/"]
COPY ["Identity.Infrastructure/Identity.Infrastructure.csproj", "Identity.Infrastructure/"]
COPY ["Identity.Create/Identity.Create.csproj", "Identity.Create/"]
RUN dotnet restore "Identity.API/Identity.csproj"
RUN dotnet restore "Identity.Create/Identity.Create.csproj"
COPY . .
WORKDIR "/src/Identity.API"
RUN dotnet build "Identity.csproj" -c ${DOT_NET_BUILD_CONFIG} -o /app/build/identity

WORKDIR "/src/Identity.Create"
RUN dotnet build "Identity.Create.csproj" -c ${DOT_NET_BUILD_CONFIG} -o /app/build/create

FROM build AS publish
ARG DOT_NET_BUILD_CONFIG=Release
WORKDIR "/src/Identity.API"
RUN dotnet publish "Identity.csproj" -c ${DOT_NET_BUILD_CONFIG} -o /app/publish/identity

WORKDIR "/src/Identity.Create"
RUN dotnet publish "Identity.Create.csproj" -c ${DOT_NET_BUILD_CONFIG} -o /app/publish/create

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "identity/Identity.dll"]
