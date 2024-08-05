FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Kattbot/Kattbot.csproj", "Kattbot/"]
COPY ["Kattbot.Common/Kattbot.Common.csproj", "Kattbot.Common/"]
COPY ["Kattbot.Data/Kattbot.Data.csproj", "Kattbot.Data/"]
COPY ["Kattbot.Data.Migrations/Kattbot.Data.Migrations.csproj", "Kattbot.Data.Migrations/"]
RUN dotnet restore "./Kattbot/Kattbot.csproj"
COPY . .
RUN dotnet build "./Kattbot/Kattbot.csproj" -c $BUILD_CONFIGURATION -o /output/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Kattbot/Kattbot.csproj" -c $BUILD_CONFIGURATION -o /output/publish /p:UseAppHost=false

FROM build AS migrations
RUN dotnet tool restore
RUN dotnet ef migrations script --idempotent -p Kattbot.Data.Migrations -o /output/migrations/database_migration.sql
COPY --from=build /src/scripts/kattbot-backup-db.sh /output/migrations/

# TODO try to bundle the migrations as an executable
# RUN dotnet ef migrations bundle -p Kattbot.Data.Migrations -r linux-x64 -o /output/migrations/efbundle

FROM base AS final
WORKDIR /app
COPY --from=publish /output/publish .
COPY --from=migrations /output/migrations ./migrations/
ENTRYPOINT ["dotnet", "Kattbot.dll"]