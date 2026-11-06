FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY TVSApp.Domain/TVSApp.Domain.csproj TVSApp.Domain/
COPY TVSApp.Application/TVSApp.Application.csproj TVSApp.Application/
COPY TVSApp.Infrastructure/TVSApp.Infrastructure.csproj TVSApp.Infrastructure/
COPY TVSApp.Api/TVSApp.Api.csproj TVSApp.Api/
RUN dotnet restore TVSApp.Api/TVSApp.Api.csproj

COPY TVSApp.Domain/ TVSApp.Domain/
COPY TVSApp.Application/ TVSApp.Application/
COPY TVSApp.Infrastructure/ TVSApp.Infrastructure/
COPY TVSApp.Api/ TVSApp.Api/
RUN dotnet publish TVSApp.Api/TVSApp.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends fontconfig fonts-liberation curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    TZ=America/Sao_Paulo

EXPOSE 8080
USER app
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=5 \
    CMD curl -fsS http://localhost:8080/ || exit 1

ENTRYPOINT ["dotnet", "TVSApp.Api.dll"]
