# Multi-stage Dockerfile for TicTacToe API optimized for Render.com

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["TicTacToeApi/TicTacToeApi.csproj", "TicTacToeApi/"]
RUN dotnet restore "TicTacToeApi/TicTacToeApi.csproj"

# Copy everything else and build
COPY TicTacToeApi/. TicTacToeApi/
WORKDIR "/src/TicTacToeApi"
RUN dotnet build "TicTacToeApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TicTacToeApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Render.com provides PORT environment variable dynamically
# The app will listen on the port specified by $PORT or default to 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "TicTacToeApi.dll"]
