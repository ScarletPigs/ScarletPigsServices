# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the main project file
COPY ["Piglet-API.csproj", "./"]

# Copy the domain models project file
COPY ["PigletDomainModels/Piglet-Domain-Models.csproj", "PigletDomainModels/"]

# Restore dependencies
RUN dotnet restore "Piglet-API.csproj"

# Copy all source code
COPY . .

# Build the project
RUN dotnet build "Piglet-API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Piglet-API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Piglet-API.dll"]
