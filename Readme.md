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

Kattbot is built on top of [DSharp+](https://dsharpplus.github.io/index.html)