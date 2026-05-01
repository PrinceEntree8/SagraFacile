# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution file
COPY SagraFacile.slnx ./

# Copy project files referenced by the solution
COPY src/SagraFacile.Domain/SagraFacile.Domain.csproj ./src/SagraFacile.Domain/
COPY src/SagraFacile.Application/SagraFacile.Application.csproj ./src/SagraFacile.Application/
COPY src/SagraFacile.Infrastructure/SagraFacile.Infrastructure.csproj ./src/SagraFacile.Infrastructure/
COPY src/SagraFacile.Web/SagraFacile.Web.csproj ./src/SagraFacile.Web/
COPY src/SagraFacile.ServiceDefaults/SagraFacile.ServiceDefaults.csproj ./src/SagraFacile.ServiceDefaults/
COPY src/SagraFacile.AppHost/SagraFacile.AppHost.csproj ./src/SagraFacile.AppHost/
COPY tests/SagraFacile.Application.Tests/SagraFacile.Application.Tests.csproj ./tests/SagraFacile.Application.Tests/
COPY tests/SagraFacile.Infrastructure.Tests/SagraFacile.Infrastructure.Tests.csproj ./tests/SagraFacile.Infrastructure.Tests/

# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY . .

# Build the application
WORKDIR /src/src/SagraFacile.Web
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SagraFacile.Web.dll"]
