FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Kattbot/Kattbot.csproj", "Kattbot/"]
COPY ["Kattbot.Common/Kattbot.Common.csproj", "Kattbot.Common/"]
COPY ["Kattbot.Data/Kattbot.Data.csproj", "Kattbot.Data/"]
RUN dotnet restore "./Kattbot/Kattbot.csproj"
COPY . .
RUN dotnet build "./Kattbot/Kattbot.csproj"

ENTRYPOINT ["dotnet", "run", "--project", "Kattbot/Kattbot.csproj"]