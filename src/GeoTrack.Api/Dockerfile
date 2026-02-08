# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY global.json ./
COPY GeoTrack.sln ./
COPY Directory.Packages.props ./
COPY src/GeoTrack.Api/GeoTrack.Api.csproj src/GeoTrack.Api/
COPY src/GeoTrack.Application/GeoTrack.Application.csproj src/GeoTrack.Application/
COPY src/GeoTrack.Domain/GeoTrack.Domain.csproj src/GeoTrack.Domain/
COPY src/GeoTrack.Infrastructure/GeoTrack.Infrastructure.csproj src/GeoTrack.Infrastructure/
COPY tests/GeoTrack.IntegrationTests/GeoTrack.IntegrationTests.csproj tests/GeoTrack.IntegrationTests/
COPY tests/GeoTrack.UnitTests/GeoTrack.UnitTests.csproj tests/GeoTrack.UnitTests/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Build and publish
WORKDIR /src/src/GeoTrack.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Run the app
ENTRYPOINT ["dotnet", "GeoTrack.Api.dll"]
