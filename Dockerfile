# Use the .NET 10.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for dependency resolution
COPY KSS.Api/KSS.Api.csproj ./KSS.Api/
COPY KSS.Data/KSS.Data.csproj ./KSS.Data/
COPY KSS.Dto/KSS.Dto.csproj ./KSS.Dto/
COPY KSS.Entity/KSS.Entity.csproj ./KSS.Entity/
COPY KSS.Helper/KSS.Helper.csproj ./KSS.Helper/
COPY KSS.Repository/KSS.Repository.csproj ./KSS.Repository/
COPY KSS.Service/KSS.Service.csproj ./KSS.Service/

# Restore NuGet packages for the API project (which will restore all dependencies)
WORKDIR /src/KSS.Api
RUN dotnet restore

# Copy all source code (maintains project structure)
WORKDIR /src
COPY . .

# Build the API project
WORKDIR /src/KSS.Api
RUN dotnet build -c Release -o /app/build --no-restore

# Publish the application
RUN dotnet publish -c Release -o /app/publish --no-restore

# Use the .NET 10.0 ASP.NET runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create directory for configuration files
RUN mkdir -p /app/config

# Copy published files from build stage
COPY --from=build /app/publish .

# Expose HTTP port
EXPOSE 8000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8000
ENV ASPNETCORE_ENVIRONMENT=Production

# Set the entry point
ENTRYPOINT ["dotnet", "KSS.Api.dll"]

