# Kattbot

## About
Discord bot for tracking and reporting server emote usage.

## Bot permissions

General: Change Nickname, View Channels

Text permissions: Send Messages, Embed links, Read Message History, Add reactions

## Local developement

### Requirements

* [.NET 5 SDK](https://dotnet.microsoft.com/download/visual-studio-sdks)
* [Visual Studio](https://visualstudio.microsoft.com/) (or any other preferred editor + dotnet command line tool)
* [PostgreSQL 12+](https://www.postgresql.org/)

### Build from Visual Studio

Build solution

### Build from dotnet command line tool

`dotnet build`

### Connection string format
`Server=_DB_SERVER_IP_;Database=_DB_NAME_;User Id=_DB_USER_;Password=_DB_PASSWORD_;`

### Secrets
Connection string 

`"Kattbot:ConnectionString" "CONN_STRING_GOES_HERE"​`

Bot token

`"Kattbot:BotToken" "TOKEN_GOES_HERE"​`

## Credits

* [DSharp+](https://github.com/DSharpPlus/DSharpPlus) .net discord wrapper
* [ImageSharp](https://github.com/SixLabors/ImageSharp) used for image manipulation
* [CommandLineParser](https://github.com/j-maly/CommandLineParser) used for parsing bot command arguments
* [MediatR](https://github.com/jbogard/MediatR) used in CQRS pattern implementation
* [Npgsql](https://github.com/npgsql/npgsql) .net data provider for PostgreSQL