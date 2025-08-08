FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/Kattbot/Kattbot.csproj", "Kattbot/"]
COPY ["src/Kattbot.Common/Kattbot.Common.csproj", "Kattbot.Common/"]
COPY ["src/Kattbot.Data/Kattbot.Data.csproj", "Kattbot.Data/"]
COPY ["src/Kattbot.Data.Migrations/Kattbot.Data.Migrations.csproj", "Kattbot.Data.Migrations/"]
COPY [".config/dotnet-tools.json", ".config/"]
RUN dotnet restore "./Kattbot/Kattbot.csproj"
RUN dotnet restore "./Kattbot.Data.Migrations/Kattbot.Data.Migrations.csproj"
RUN dotnet tool restore
COPY ["Kattbot.sln", "."]
COPY ["stylecop.json", "."]
COPY [".editorconfig", "."]
COPY ["scripts/kattbot-backup-db.sh", "."] 
COPY src .
RUN dotnet build "./Kattbot/Kattbot.csproj" --no-restore -c $BUILD_CONFIGURATION
RUN dotnet build "./Kattbot.Data.Migrations/Kattbot.Data.Migrations.csproj" --no-restore -c $BUILD_CONFIGURATION

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Kattbot/Kattbot.csproj" --no-build -c $BUILD_CONFIGURATION -o /output/publish /p:UseAppHost=false

FROM build AS migrations
ARG BUILD_CONFIGURATION=Release
RUN dotnet ef migrations script --no-build --configuration $BUILD_CONFIGURATION --idempotent -p Kattbot.Data.Migrations -o /output/migrations/database_migration.sql
COPY --from=build /src/kattbot-backup-db.sh /output/migrations/

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=publish /output/publish .
COPY --from=migrations /output/migrations ./migrations/
USER $APP_UID
ENTRYPOINT ["dotnet", "Kattbot.dll"]
