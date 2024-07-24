FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base

# Install dependencies for Puppeteer
RUN apt-get update && apt-get install -y \
    gconf-service libasound2 libatk1.0-0 libc6 libcairo2 \
    libcups2 libdbus-1-3 libexpat1 libfontconfig1 libgbm1 libgcc1 libgconf-2-4 \
    libgdk-pixbuf2.0-0 libglib2.0-0 libgtk-3-0 libnspr4 libpango-1.0-0 \
    libpangocairo-1.0-0 libstdc++6 libx11-6 libx11-xcb1 libxcb1 libxcomposite1 \
    libxcursor1 libxdamage1 libxext6 libxfixes3 libxi6 libxrandr2 libxrender1 \
    libxss1 libxtst6 ca-certificates fonts-liberation libnss3 lsb-release \
    xdg-utils wget

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