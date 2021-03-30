## Database

#install ef-tools
dotnet tool install dotnet-ef -g

#update dev database
dotnet ef database update -p Kattbot.Data.Migrations

#update to target migration
dotnet ef database update TargetMigration -p Kattbot.Data.Migrations

#add migration
dotnet ef migrations add MigrationName -p Kattbot.Data.Migrations

#remove last migration
dotnet ef migrations remove -p Kattbot.Data.Migrations

#generate database upgrade script
dotnet ef migrations script --idempotent -o database_migration.sql -p Kattbot.Data.Migrations

#connection string format
Server=_DB_SERVER_IP_;Database=_DB_NAME_;User Id=_DB_USER_;Password=_DB_PASSWORD_

# Manual database backup on vps
sudo su - postgres
pg_dump kattbot > /tmp/kattbot.bak
exit
mv /tmp/kattbot.bak /usr/kattbot-db-backups/kattbot-iso-date-time.bak

## Publish

#publish windows release
dotnet publish -c Release -r win10-x64 --self-contained false

#publish linux release
dotnet publish -c Release -r linux-x64 --self-contained false

## User secrets

dotnet user-secrets list

# Required secrets for local development
dotnet user-secrets set "Kattbot:ConnectionString" "CONN_STRING_GOES_HERE"​
dotnet user-secrets set "Kattbot:BotToken" "TOKEN_GOES_HERE"​

## Bot

# Required bot permissions

General: Change Nickname, View Channels
Text permissions: Send Messages, Embed links, Read Message History, Add reactions