#==== BASE
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    gnupg \
    apt-transport-https \
    ca-certificates \
    adduser \
    && rm -rf /var/lib/apt/lists/*

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN /usr/sbin/adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
EXPOSE 8080
EXPOSE 8081

#==== BUILD
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . ./
#COPY ["Kasta.Web/Kasta.Web.csproj", "Kasta.Web/"]
RUN dotnet restore "Kasta.Web/Kasta.Web.csproj"
WORKDIR "/src/Kasta.Web"
RUN dotnet tool install --global nuget-license
ENV PATH="$PATH:/root/.dotnet/tools"
RUN nuget-license -i Kasta.Web.csproj -o Json -fo app-licenses.json
RUN dotnet build "Kasta.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

#==== PUBLISH
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet tool install --global nuget-license
RUN nuget-license -i Kasta.Web.csproj -o Json -fo app-licenses.json
RUN dotnet publish "Kasta.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

#==== FINAL
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kasta.Web.dll", "docker"]
