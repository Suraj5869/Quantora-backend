
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the complete solution, including all project files
COPY . .

# Restore dependencies
RUN dotnet restore Quantora.slnx

# Publish the API and its referenced projects
RUN dotnet publish src/Quantora.Api/Quantora.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Render will route traffic to this port
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Quantora.Api.dll"]
