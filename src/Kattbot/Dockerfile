FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Debug
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/Kattbot/Kattbot.csproj", "Kattbot/"]
COPY ["src/Kattbot.Common/Kattbot.Common.csproj", "Kattbot.Common/"]
COPY ["src/Kattbot.Data/Kattbot.Data.csproj", "Kattbot.Data/"]
RUN dotnet restore "./Kattbot/Kattbot.csproj"
COPY ["Kattbot.sln", "."]
COPY ["stylecop.json", "."]
COPY [".editorconfig", "."]
COPY src .
RUN dotnet build "./Kattbot/Kattbot.csproj" -c $BUILD_CONFIGURATION --no-restore
USER $APP_UID
ENTRYPOINT ["dotnet", "run", "--project", "Kattbot/Kattbot.csproj", "--no-build"]