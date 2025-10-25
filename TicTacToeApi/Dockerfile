# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["TicTacToeApi/TicTacToeApi.csproj", "TicTacToeApi/"]
RUN dotnet restore "TicTacToeApi/TicTacToeApi.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/TicTacToeApi"
RUN dotnet build "TicTacToeApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TicTacToeApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Render.com provides PORT environment variable dynamically
# The app will listen on the port specified by $PORT
EXPOSE 8080

# Run as non-root user for security
USER $APP_UID

ENTRYPOINT ["dotnet", "TicTacToeApi.dll"]