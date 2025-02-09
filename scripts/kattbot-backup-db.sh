#! /bin/bash

# create db backup in tmp folder
pg_dump kattbot > /tmp/kattbot.bak
# pg_dump --data-only --exclude-table-data=public.\"__EFMigrationsHistory\" kattbot > /tmp/kattbot.bak

# create folder if it doesn't exist
mkdir -p "$HOME/kattbot-db-backups"

# move backup file to backups folder
mv /tmp/kattbot.bak "$HOME/kattbot-db-backups/kattbot-$(date +%Y-%m-%d-%H:%M).bak"