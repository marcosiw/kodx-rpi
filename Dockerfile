FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Kodx.Rpi.slnx .
COPY src/Kodx.Rpi.Domain/Kodx.Rpi.Domain.csproj src/Kodx.Rpi.Domain/
COPY src/Kodx.Rpi.Application/Kodx.Rpi.Application.csproj src/Kodx.Rpi.Application/
COPY src/Kodx.Rpi.Infrastructure/Kodx.Rpi.Infrastructure.csproj src/Kodx.Rpi.Infrastructure/
COPY src/Kodx.Rpi.Api/Kodx.Rpi.Api.csproj src/Kodx.Rpi.Api/
RUN dotnet restore src/Kodx.Rpi.Api/Kodx.Rpi.Api.csproj

COPY src/ src/
RUN dotnet publish src/Kodx.Rpi.Api/Kodx.Rpi.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Certificados do mTLS (Grpc:Mtls, porta 8080) NÃO entram na imagem — ela é publicada
# no GHCR e chave privada de servidor não pode viajar num artefato versionado/público.
# Esperado em /app/certs via volume montado em runtime (ver docker-compose.yml e
# scripts/generate-mtls-certs.sh).
EXPOSE 8080 8081

ENTRYPOINT ["dotnet", "Kodx.Rpi.Api.dll"]
