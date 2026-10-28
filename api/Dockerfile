# Build from the repository root: docker build -f api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY api/src/CineVault.Api/CineVault.Api.csproj api/src/CineVault.Api/
RUN dotnet restore api/src/CineVault.Api/CineVault.Api.csproj
COPY api/src api/src
RUN dotnet publish api/src/CineVault.Api/CineVault.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__CineVault="Data Source=/data/cinevault.db"
COPY --from=build /app/publish .
RUN mkdir -p /data && chown -R $APP_UID /data
USER $APP_UID
VOLUME /data
EXPOSE 8080
ENTRYPOINT ["dotnet", "CineVault.Api.dll"]
