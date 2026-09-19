FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ScoreApi.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# The base image listens on port 8080 by default (runs as a non-root user)
USER $APP_UID
ENTRYPOINT ["dotnet", "ScoreApi.dll"]
