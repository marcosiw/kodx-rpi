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

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Kodx.Rpi.Api.dll"]
