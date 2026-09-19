# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for layer caching
COPY SFMA_API.slnx ./
COPY SFMA_API.Api/SFMA_API.Api.csproj SFMA_API.Api/
COPY SFMA_API.Data/SFMA_API.Data.csproj SFMA_API.Data/
COPY SFMA_API.Logger/SFMA_API.Logger.csproj SFMA_API.Logger/
COPY SFMA_API.Models/SFMA_API.Models.csproj SFMA_API.Models/
COPY SFMA_API.Services/SFMA_API.Services.csproj SFMA_API.Services/

RUN dotnet restore SFMA_API.slnx

# Copy entire source code
COPY . .

# Build and publish release output
WORKDIR /src/SFMA_API.Api
RUN dotnet publish SFMA_API.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SFMA_API.Api.dll"]
