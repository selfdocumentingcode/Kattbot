# Kattbot

## About
It can haz ur emoji usage.

## Database

### Install ef-tools
`dotnet tool install dotnet-ef -g`

### Update dev database
`dotnet ef database update -p Kattbot.Data.Migrations`

### Update to target migration
`dotnet ef database update TargetMigration -p Kattbot.Data.Migrations`

### Add migration
`dotnet ef migrations add MigrationName -p Kattbot.Data.Migrations`

### Remove last migration
`dotnet ef migrations remove -p Kattbot.Data.Migrations`

### Generate database upgrade script
`dotnet ef migrations script --idempotent -o database_migration.sql -p Kattbot.Data.Migrations`

### Connection string format
`Server=_DB_SERVER_IP_;Database=_DB_NAME_;User Id=_DB_USER_;Password=_DB_PASSWORD_`

## Publish

### Publish windows release
`dotnet publish -c Release -r win10-x64 --self-contained false`

### Publish linux release
`dotnet publish -c Release -r linux-x64 --self-contained false`

## User secrets

### List secrets
`dotnet user-secrets list`

## Required secrets for local development
`dotnet user-secrets set "Kattbot:ConnectionString"" CONN_STRING_GOES_HERE"​`

`dotnet user-secrets set "Kattbot:BotToken" "TOKEN_GOES_HERE"​`

## Bot

General: Change Nickname, View Channels

Text permissions: Send Messages, Embed links, Read Message History, Add reactions