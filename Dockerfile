# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CompraProgramada.sln .
COPY src/CompraProgramada.Domain/CompraProgramada.Domain.csproj src/CompraProgramada.Domain/
COPY src/CompraProgramada.Application/CompraProgramada.Application.csproj src/CompraProgramada.Application/
COPY src/CompraProgramada.Infrastructure/CompraProgramada.Infrastructure.csproj src/CompraProgramada.Infrastructure/
COPY src/CompraProgramada.Api/CompraProgramada.Api.csproj src/CompraProgramada.Api/
RUN dotnet restore src/CompraProgramada.Api/CompraProgramada.Api.csproj

COPY src/ src/
RUN dotnet publish src/CompraProgramada.Api/CompraProgramada.Api.csproj -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/cotacoes

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "CompraProgramada.Api.dll"]