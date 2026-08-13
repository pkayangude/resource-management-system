FROM registry.redhat.io/ubi9/dotnet-80-runtime:latest AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

FROM registry.redhat.io/ubi9/dotnet-80:latest AS build
WORKDIR /src
COPY ["src/ResourceManagement.API/ResourceManagement.API.csproj", "src/ResourceManagement.API/"]
COPY ["src/ResourceManagement.Core/ResourceManagement.Core.csproj", "src/ResourceManagement.Core/"]
COPY ["src/ResourceManagement.Infrastructure/ResourceManagement.Infrastructure.csproj", "src/ResourceManagement.Infrastructure/"]
RUN dotnet restore "src/ResourceManagement.API/ResourceManagement.API.csproj"
COPY . .
WORKDIR "/src/src/ResourceManagement.API"
RUN dotnet build "ResourceManagement.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResourceManagement.API.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
# Non-root user (IBM security requirement: NEVER run as root)
USER 1001
COPY --from=publish --chown=1001:1001 /app/publish .
ENTRYPOINT ["dotnet", "ResourceManagement.API.dll"]
