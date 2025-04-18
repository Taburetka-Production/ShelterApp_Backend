# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies first for layer caching
COPY ["ShelterApp/ShelterApp.csproj", "./"]
RUN dotnet restore "ShelterApp.csproj"

# Copy the rest of the source code
COPY ./ShelterApp .
WORKDIR "/src/."
RUN dotnet build "ShelterApp.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "ShelterApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Create the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose the port the application listens on (check your Program.cs or launchSettings.json)
# Common ports are 80 (HTTP) or 443 (HTTPS), or 5000/5001 during development
EXPOSE 5251 7118

ENTRYPOINT ["dotnet", "ShelterApp.dll"]
