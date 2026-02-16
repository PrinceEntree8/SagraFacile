# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution file
COPY SagraFacile.sln ./

# Copy project files
COPY src/SagraFacile.Web/SagraFacile.Web/SagraFacile.Web.csproj ./src/SagraFacile.Web/SagraFacile.Web/
COPY src/SagraFacile.Web/SagraFacile.Web.Client/SagraFacile.Web.Client.csproj ./src/SagraFacile.Web/SagraFacile.Web.Client/

# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY . .

# Build the application
WORKDIR /src/src/SagraFacile.Web/SagraFacile.Web
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
