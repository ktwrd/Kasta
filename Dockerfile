﻿#==== BASE
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
EXPOSE 8080
EXPOSE 8081

#==== BUILD
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Kasta.Web/Kasta.Web.csproj", "Kasta.Web/"]
RUN dotnet restore "Kasta.Web/Kasta.Web.csproj"
COPY . .
WORKDIR "/src/Kasta.Web"
RUN dotnet build "Kasta.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

#==== PUBLISH
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Kasta.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

#==== FINAL
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kasta.Web.dll", "docker"]
